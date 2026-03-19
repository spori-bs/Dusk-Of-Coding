# Phase 2 Master Prompt: Execution Engine, Persistence & Domain Hardening

You are a Principal Software Architect specializing in .NET, Secure Code Execution, and Clean Architecture.

## Objective

Evolve the **Dusk of Coding** platform from a scaffolded POC with a stubbed Execution API into a fully functional practice environment. The platform must **compile student code, generate and run xUnit tests**, and **return pass/fail results** — all without introducing a remote code execution vulnerability. Feedback must be **persisted** and the domain model must be **hardened**.

## Context

The application is a Modular Monolith built with .NET 10 & .NET Aspire consisting of:

| Project | Role |
|---------|------|
| `DuskOfCoding.AppHost` | Aspire orchestrator (RabbitMQ, Keycloak, ExecutionApi, WebApi, WebUi, TutorWorker) |
| `DuskOfCoding.Domain` | Entities (`TaskDefinition`, `Submission`), interfaces, models (`Feedback`, `ExecutionResult`) |
| `DuskOfCoding.Application` | Services (`SubmissionService`, `TaskService`) |
| `DuskOfCoding.Infrastructure` | EF Core (SQLite), RabbitMQ messaging, HTTP execution engine |
| `DuskOfCoding.WebApi` | Minimal API endpoints + SignalR TutorHub |
| `DuskOfCoding.WebUi` | Blazor Server frontend (Monaco editor, Practice page, TutorTerminal) |
| `DuskOfCoding.ExecutionApi` | **Stubbed** — always returns simulated success |
| `DuskOfCoding.Execution.Contracts` | Shared DTOs (`ExecutionRequest`, `ExecutionResponse`) |
| `DuskOfCoding.TutorWorker` | Background AI tutor with Polly resilience, MCP tools |

### What Already Works
- Keycloak OIDC login/registration, JWT-secured APIs
- Full RabbitMQ pipeline: submission → TutorWorker → response → SignalR → TutorTerminal
- Localization (HU/EN), dark glassmorphism UI, Monaco code editor
- Roslyn syntax checking (client-side + server-side pre-execution)

### What Does NOT Work
- **ExecutionApi** is a hard-coded stub — no real compilation, no test execution
- **Feedback is in-memory only** (`ConcurrentDictionary<Guid, Feedback>` in `SubmissionService`)
- `Submission.Status` uses magic strings — no enum
- Domain entity mutability is inconsistent (`init` vs `set`)
- Old `PracticePlatform.*.http` file names remain

## Security Architecture — Sandboxed Execution via Roslyn Whitelist

> **Design Principle**: The compile-time whitelist IS the security boundary. If dangerous code can't compile, it can't run. No containers needed.

The ExecutionApi does **not** shell out to Docker/Podman. Instead, it uses a **defense-in-depth** approach:

### Layer 1: Compile-Time API Whitelist (Primary Security Boundary)

Student code is compiled via Roslyn with **only these assembly references** available:

```
✅ ALLOWED (whitelisted MetadataReferences):
   - System.Runtime
   - System.Console (Console.WriteLine only)
   - System.Collections
   - System.Linq
   - System.Text
   - netstandard
   - mscorlib / System.Private.CoreLib

❌ BLOCKED (not referenced → compile error if student tries to use):
   - System.IO.FileSystem          → no file access
   - System.Net.*                  → no network access
   - System.Diagnostics.Process    → no process spawning
   - System.Reflection.Emit        → no dynamic code generation
   - System.Runtime.InteropServices → no P/Invoke / native calls
   - System.Environment (Exit, etc.) → no host manipulation
   - System.Threading.Thread       → no raw thread spawning
```

If the student code references **any** type outside the whitelist, Roslyn compilation fails with a clear error message. The code is **never executed**.

### Layer 2: Roslyn Syntax Analysis (Defense-in-Depth)

Before compilation, walk the syntax tree to reject:
- `unsafe` blocks and pointer types
- `#pragma` directives
- `DllImport` / `LibraryImport` attributes
- `extern` method declarations
- Any `global::` qualified access attempting to bypass using restrictions
- **Reflection**: `typeof()`, `.GetType()`, `System.Reflection`, `Activator` (prevents bypassing the whitelist via `System.Private.CoreLib`).
- **Control Flow Abuse**: `goto` statements, and broad catch blocks (`catch (Exception)`, `catch { }`) to prevent swallowing cancellation exceptions.

### Layer 3: Roslyn Syntax Rewriting (Injected Safety Hooks)

Before compilation, the student's syntax tree is **automatically rewritten** to inject safety checks. This is invisible to the student.

#### 3a. Loop Cancellation Injection

Inject `_cancellationToken.ThrowIfCancellationRequested();` as the **first statement** in every loop body (`while`, `for`, `foreach`, `do-while`). This makes the 5-second timeout effective even against tight infinite loops.

```csharp
// Student wrote:
while (true) { x++; }

// After automatic rewriting:
while (true) { __cts.ThrowIfCancellationRequested(); x++; }
```

#### 3b. Recursion Depth Counter Injection

Inject a **call-depth counter** into every method. If recursion exceeds a configurable limit (e.g. 200), throw an exception. This prevents `StackOverflowException` — which is **fatal in .NET** and would crash the entire ExecutionApi process.

```csharp
// Student wrote:
public int Factorial(int n) { return n <= 1 ? 1 : n * Factorial(n - 1); }

// After automatic rewriting:
public int Factorial(int n) {
    if (++__callDepth > 200) throw new InvalidOperationException("Maximum recursion depth exceeded.");
    try { return n <= 1 ? 1 : n * Factorial(n - 1); }
    finally { __callDepth--; }
}
```

#### 3c. Implementation Notes

- Inject a `[ThreadStatic] static int __callDepth;` field and a `static CancellationToken __cts;` field into the student's class.
- Set `__cts` from the host before invoking tests.
- The rewriter is a standard Roslyn `CSharpSyntaxRewriter` that visits `WhileStatementSyntax`, `ForStatementSyntax`, `ForEachStatementSyntax`, `DoStatementSyntax`, and `MethodDeclarationSyntax`.

### Layer 4: Runtime Protection

- **Timeout**: Execute tests on a dedicated `Thread` with `CancellationTokenSource` (5 seconds). The injected loop checks make this effective.
- **Memory monitoring**: A background timer polls `GC.GetAllocatedBytesForCurrentThread()` on the executing thread at 200ms intervals. If the delta exceeds **50 MB**, trigger the `CancellationTokenSource`. (Using `GetTotalAllocatedBytes` would incorrectly blend memory from concurrent submissions).
- **Thread Exhaustion Risk**: Acknowledge that an infinite blocking call without loops (e.g., an expensive LINQ query `Enumerable.Range(0, int.MaxValue).ToArray()`) will hang the worker thread. The 5s host timeout will abandon the execution, but the native thread will leak, preventing `AssemblyLoadContext` unloading. This is an accepted risk of in-process execution without containers.
- **AssemblyLoadContext**: Load the compiled assembly in an **unloadable** `AssemblyLoadContext` — after test execution, unload and GC the entire context.
- **Concurrency limit**: Use `SemaphoreSlim` to limit parallel executions (configurable, default: 5), preventing resource exhaustion.

### What This Prevents

| Threat | How It's Blocked |
|--------|-----------------|
| Read/write files on server | `System.IO` not referenced → won't compile |
| Make HTTP calls / exfiltrate data | `System.Net` not referenced → won't compile |
| Spawn processes / run commands | `System.Diagnostics.Process` not referenced → won't compile |
| Fork bomb / infinite threads | `System.Threading.Thread` not referenced → won't compile |
| P/Invoke native code | Syntax analysis rejects `DllImport`, `unsafe` |
| Reflection / CoreLib Escape | Syntax analysis rejects `typeof`, `.GetType()`, `System.Reflection` |
| Infinite loop (`while(true){}`) | Injected `ThrowIfCancellationRequested()` + blocked `catch` + 5s timeout |
| Infinite recursion (`Foo(){Foo();}`) | Injected depth counter throws at limit 200 (prevents fatal StackOverflow) |
| Memory bomb (`s += "boom"` in loop) | Memory monitor triggers cancellation at 50 MB delta on current thread |
| Concurrent flood (50 users) | SemaphoreSlim limits parallel executions |

## Technical Constraints

* **Runtime**: .NET 10
* **Execution**: Roslyn in-process compilation + `AssemblyLoadContext` isolation (NO Docker/Podman)
* **Messaging**: Official `RabbitMQ.Client` (no MassTransit)
* **Background Logic**: Standard `BackgroundService`
* **Resiliency**: Polly v8+ Strategy-based API
* **ORM**: Entity Framework Core (SQLite for dev, PostgreSQL for prod)
* **Prefer**: `Microsoft.Extensions.*` standard libraries
* **Aspire**: Wire everything in the AppHost
* **Deployment targets**: On-premise (VM) and Azure (Container Apps / App Service)

---

## The `phase2-context.md` Protocol

You **must** maintain a file named `phase2-context.md` in the project root. Update it at the end of every response and read it at the start.

---

## Work in Phases

### PHASE 1: Domain Model Hardening

Foundational cleanup that all subsequent phases depend on.

1. **`SubmissionStatus` enum**: Create `DuskOfCoding.Domain/Enums/SubmissionStatus.cs` with values: `Pending`, `Executing`, `CompilationFailed`, `TestsFailed`, `Success`, `Error`.
2. **Fix `Submission` entity**: Change `Status` from `string` to `SubmissionStatus`. Audit `init` vs `set` — properties that are mutated after construction (`Status`, `CompletedAt`) must use `set`; immutable properties (`Id`, `TaskId`, `UserId`, `SourceCode`, `CreatedAt`) keep `init`.
3. **Fix `TaskDefinition` entity**: Properties that can be updated (`Title`, `Description`, `DifficultyLevel`, `Tags`, `TestBundleReference`) should use `set`, not `init`.
4. **EF Migration**: Generate a new migration for the `SubmissionStatus` enum change.
5. **Propagate changes**: Update `SubmissionService`, WebApi endpoints, DTOs, and Blazor components to use the new enum instead of magic strings.
6. **Rename leftovers**: Rename `PracticePlatform.WebApi.http` → `DuskOfCoding.WebApi.http` and `PracticePlatform.ExecutionApi.http` → `DuskOfCoding.ExecutionApi.http`.

### PHASE 2: Feedback Persistence

Replace the in-memory `ConcurrentDictionary` with proper EF Core persistence.

1. **`FeedbackRecord` entity**: Create `DuskOfCoding.Domain/Entities/FeedbackRecord.cs` as a database entity with: `Id`, `SubmissionId` (FK → `Submission`), `IsSuccess`, `Summary`, `CompilationMessagesJson` (serialized), `TestMessagesJson` (serialized), `AiReviewRemarks`, `CreatedAt`.
2. **Navigation property**: Add `FeedbackRecord? Feedback` navigation to `Submission`.
3. **EF Configuration**: Configure the `FeedbackRecord` table and the 1:1 relationship in `AppDbContext`.
4. **EF Migration**: Generate migration for the new table.
5. **Update `SubmissionService`**: Remove the `static ConcurrentDictionary`. Persist `FeedbackRecord` via the repository after every submission evaluation. Map between `Feedback` (domain model) and `FeedbackRecord` (entity).
6. **Update DTOs & API**: Ensure `GET /submissions/{id}` returns persisted feedback.

### PHASE 3: Real Execution Engine (Roslyn Sandbox)

Replace the stubbed `ExecutionApi` with a real Roslyn-based execution engine.

1. **`TestBundleReference` format**: Each `TaskDefinition` stores its `TestBundleReference` as an **inline xUnit test class template** in C#. The template uses `{StudentCode}` as a namespace/class reference placeholder. Example:

   ```csharp
   using Xunit;

   public class SolutionTests
   {
       [Fact]
       public void Add_TwoPositiveNumbers_ReturnsSum()
       {
           var result = Solution.Add(2, 3);
           Assert.Equal(5, result);
       }

       [Fact]
       public void Add_NegativeNumber_ReturnsCorrectSum()
       {
           var result = Solution.Add(-1, 5);
           Assert.Equal(4, result);
       }
   }
   ```

2. **Whitelisted compilation**:
   - Build a fixed set of `MetadataReference`s (the whitelist from the Security Architecture section).
   - Add xUnit references (`xunit.core`, `xunit.assert`) to the compilation.
   - Combine the student's source code + the test class into a single Roslyn compilation.
   - If compilation fails → return `ExecutionResponse` with `Compilation.Succeeded = false` and error messages. **Do not execute.**

3. **Syntax analysis gate** (before compilation):
   - Walk the student's `SyntaxTree` to reject `unsafe`, `DllImport`, `extern`, `#pragma`, pointer types, `goto`, broad `catch` blocks, and any Reflection namespaces/methods (`typeof`, `GetType`, `Activator`).
   - Return clear error if any are found.

4. **Syntax rewriting** (after analysis, before compilation):
   - Implement a `SafetyRewriter : CSharpSyntaxRewriter` that:
     - Injects `__cts.ThrowIfCancellationRequested();` as the first statement of every loop body (`while`, `for`, `foreach`, `do`).
     - Wraps every method body with a recursion depth check (`if (++__callDepth > 200) throw ...` / `finally { __callDepth--; }`).
     - Injects `[ThreadStatic] static int __callDepth;` and `static CancellationToken __cts;` fields into the student's class.

5. **Test execution**:
   - Load the compiled assembly into an **unloadable `AssemblyLoadContext`**.
   - Set the injected `__cts` field via reflection from the host's `CancellationToken`.
   - Use pure **Reflection** to discover and invoke `[Fact]` / `[Theory]` methods (since `xunit.runner.utility` expects tests to be saved to physical `.dll` files on disk).
   - Capture pass/fail status and assertion messages per test.
   - Enforce a **5-second timeout** via `CancellationTokenSource`. The injected loop checks make this effective against tight loops.
   - **Memory monitoring**: Start a background timer polling `GC.GetAllocatedBytesForCurrentThread()` on the specific execution thread at 200ms intervals. If delta exceeds **50 MB**, trigger the `CancellationTokenSource`.

6. **Concurrency limit**: Use `SemaphoreSlim` (configurable, default: 5) to limit parallel executions.

7. **Cleanup**: Unload the `AssemblyLoadContext` and force GC after each execution. Reset `__callDepth`.

8. **Health check**: Keep the `/health` endpoint functional.

### PHASE 4: Integration Wiring

Connect the new execution engine to the rest of the system.

1. **Update `ICodeExecutionEngine`**: Ensure the infrastructure's HTTP execution engine sends the correct `ExecutionRequest` (including `TestBundle` from the task's `TestBundleReference`).
2. **Update `SubmissionService`**: Adjust the submission flow to properly hydrate the `TestBundleReference` from the `TaskDefinition`.
3. **Update `TaskEditor.razor`**: Add a code editor (or textarea) for entering/editing the `TestBundleReference` (the xUnit test template) on tasks.
4. **Seed data**: Create at least one test task with a real `TestBundleReference` so end-to-end testing is possible. Example task: "Write an `Add(int a, int b)` method that returns the sum."

### PHASE 5: End-to-End Verification

1. **Happy path**: Submit valid C# code for a seeded task → verify real compilation + all tests pass → verify feedback is persisted → verify TutorWorker AI response arrives via SignalR.
2. **Compilation failure**: Submit code with syntax errors → verify compilation errors appear in the response.
3. **Test failure**: Submit code that compiles but fails tests → verify test failure messages with assertion details.
4. **Security — banned API**: Submit code that tries `File.ReadAllText("C:\\...")` → verify it fails at compilation (not at runtime).
5. **Security — infinite loop**: Submit code with `while(true){}` → verify 5-second timeout kicks in via injected cancellation.
6. **Security — infinite recursion**: Submit code with `void Foo(){Foo();}` → verify depth limit exception (not a process-crashing StackOverflow).
7. **Security — memory bomb**: Submit code with `string s=""; while(true) s+="x";` → verify memory monitor triggers cancellation.
8. **Persistence**: Restart the API → verify previously submitted feedback is still available via `GET /submissions/{id}`.

---

## Deliverables per Phase

1. **Working code** with proper compilation (0 errors, 0 warnings).
2. **Updated `phase2-context.md`** with phase status, decisions, and resumption instructions.
3. **EF Migrations** for schema changes (Phases 1 & 2).
4. **Updated README.md** architecture description if needed.

---

## Always

* **Prefer standard libraries** (`Microsoft.Extensions.*`).
* **Aspire Integration**: Wire everything in the `AppHost`.
* **No Stuck Requests**: Every submission must eventually return feedback, even on failure.
* **No Docker/Podman**: All execution is in-process via Roslyn + `AssemblyLoadContext`.
* **Clean Architecture**: Domain has no infrastructure dependencies. Application orchestrates. Infrastructure implements.
* **Security first**: The assembly reference whitelist is the primary security boundary. Never execute code that hasn't passed both syntax analysis and whitelisted compilation.

---

### Current Status

* **Base App**: All prior features (RabbitMQ, TutorWorker, Keycloak, Localization, Blazor UI) are complete.
* **Next Task**: Begin with **Phase 1 (Domain Model Hardening)** — it unblocks all other phases.

**Please start by acknowledging the architecture and generating the initial `phase2-context.md`, then begin Phase 1 code changes.**
