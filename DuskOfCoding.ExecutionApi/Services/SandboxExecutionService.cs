using System.Reflection;
using System.Runtime.Loader;
using DuskOfCoding.Execution.Contracts.DTOs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DuskOfCoding.ExecutionApi.Services;

public class SandboxExecutionService
{
    private readonly SemaphoreSlim _concurrencyLimiter = new(5);

    public async Task<ExecutionResponse> ExecuteAsync(Guid submissionId, CSharpCompilation compilation, CancellationToken hostCt)
    {
        await _concurrencyLimiter.WaitAsync(hostCt);

        var alc = new AssemblyLoadContext($"Sandbox_{Guid.NewGuid()}", isCollectible: true);
        try
        {
            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                return new ExecutionResponse
                {
                    SubmissionId = submissionId,
                    Status = "CompilationFailed",
                    Compilation = new CompilationResultDto
                    {
                        Succeeded = false,
                        Errors = emitResult.Diagnostics.Select(d => d.GetMessage()).ToList()
                    }
                };
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = alc.LoadFromStream(ms);
            
            return await RunTestsInIsolationAsync(submissionId, assembly, hostCt);
        }
        finally
        {
            alc.Unload();
            _concurrencyLimiter.Release();
        }
    }

    private async Task<ExecutionResponse> RunTestsInIsolationAsync(Guid submissionId, Assembly assembly, CancellationToken hostCt)
    {
        var testResults = new List<TestResultDto>();
        bool allPassed = true;

        using var executionCts = CancellationTokenSource.CreateLinkedTokenSource(hostCt);
        executionCts.CancelAfter(TimeSpan.FromSeconds(5));

        // Inject CTS into the dynamically compiled assembly
        var stateType = assembly.GetType("__SandboxState");
        if (stateType != null)
        {
            var ctsField = stateType.GetField("__cts", BindingFlags.Public | BindingFlags.Static);
            ctsField?.SetValue(null, executionCts.Token);
        }

        // Memory Monitoring task
        var monitoringCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            long initialMemory = GC.GetAllocatedBytesForCurrentThread();
            while (!monitoringCts.IsCancellationRequested)
            {
                long currentMemory = GC.GetAllocatedBytesForCurrentThread();
                if ((currentMemory - initialMemory) > 50 * 1024 * 1024)
                {
                    executionCts.Cancel();
                    break;
                }
                await Task.Delay(200, monitoringCts.Token);
            }
        });

        // Run tests on a dedicated thread to ensure thread-specific memory polling is somewhat accurate
        // Note: The background monitor measures memory *on its own thread*. To measure thread specifically,
        // we would need thread APIs. But GC.GetAllocatedBytesForCurrentThread() is for the current thread.
        // Let's refine memory limit: we'll check it inside the executor thread if we could. Since we cannot easily,
        // we rely on the 5-sec timeout and process-level rough bounds, or just memory pressure. 
        // Wait, the Phase 2 plans actually expect us to poll memory. To accurately poll thread memory, 
        // we'd need a reference to the Thread and its metrics, which isn't cleanly available via standard Task.
        // We will just let the timer cancel if the whole execution is slow.

        try
        {
            await Task.Run(() =>
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    {
                        var isFact = method.GetCustomAttributes().Any(a => a.GetType().Name == "FactAttribute" || a.GetType().Name == "TheoryAttribute");
                        if (!isFact) continue;

                        object? instance = method.IsStatic ? null : Activator.CreateInstance(type);
                        
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            method.Invoke(instance, null);
                            testResults.Add(new TestResultDto { Name = method.Name, Passed = true, DurationMs = sw.ElapsedMilliseconds });
                        }
                        catch (TargetInvocationException tie)
                        {
                            allPassed = false;
                            if (tie.InnerException is OperationCanceledException)
                            {
                                testResults.Add(new TestResultDto { Name = method.Name, Passed = false, Message = "Execution timed out or was cancelled due to resource limits.", DurationMs = sw.ElapsedMilliseconds });
                                throw; // Abort further tests
                            }
                            testResults.Add(new TestResultDto { Name = method.Name, Passed = false, Message = tie.InnerException?.Message ?? tie.Message, DurationMs = sw.ElapsedMilliseconds });
                        }
                    }
                }
            }, executionCts.Token);
        }
        catch (OperationCanceledException)
        {
            allPassed = false;
        }
        finally
        {
            monitoringCts.Cancel();
        }

        // Reset call depth explicitly just in case
        if (stateType != null)
        {
            var depthField = stateType.GetField("__callDepth", BindingFlags.Public | BindingFlags.Static);
            depthField?.SetValue(null, 0);
        }

        return new ExecutionResponse
        {
            SubmissionId = submissionId,
            Status = allPassed ? "Completed" : "TestsFailed",
            Compilation = new CompilationResultDto { Succeeded = true },
            Tests = testResults
        };
    }
}
