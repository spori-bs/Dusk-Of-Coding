using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Models;
using DuskOfCoding.Infrastructure.Prompts;

namespace DuskOfCoding.Infrastructure.Services;

public class AiReviewService : IAIReviewService
{
    private readonly IChatClient _chatClient;

    public AiReviewService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<Feedback> EnrichFeedbackAsync(
        TaskDefinition task, 
        Submission submission, 
        ExecutionResult executionResult, 
        string? preferredLanguage = null,
        CancellationToken ct = default)
    {
        bool isSuccess = executionResult.CompilationSucceeded && executionResult.Tests.All(t => t.Passed);
        bool isHungarian = preferredLanguage?.StartsWith("hu", StringComparison.OrdinalIgnoreCase) == true;

        // ── Prompt-injection guard ───────────────────────────────────────────────
        var injectionWarning = PromptInjectionGuard.TryDetect(submission.SourceCode, isHungarian);
        if (injectionWarning is not null)
        {
            return new Feedback
            {
                IsSuccess = false,
                Summary = isHungarian ? "Prompt injekciós kísérlet blokkolva." : "Prompt injection attempt blocked.",
                CompilationMessages = Array.Empty<string>(),
                TestMessages = Array.Empty<string>(),
                AiReviewRemarks = injectionWarning
            };
        }
        // ────────────────────────────────────────────────────────────────────────

        var systemPrompt = SocraticTutorPrompt.GetSystemPrompt(preferredLanguage);

        var hasNamespace = submission.SourceCode.Contains("namespace ", StringComparison.OrdinalIgnoreCase);
        var namespaceWarning = !hasNamespace && !string.IsNullOrWhiteSpace(task.Namespace)
            ? (isHungarian
                ? $"\n\n⚠️ FIGYELEM: A beküldött kód nem tartalmaz névtér deklarációt (`namespace`). A feladat elvárt névtere: `{task.Namespace}`. Mindenképpen hívd fel a hallgató figyelmét erre a hiányosságra!"
                : $"\n\n⚠️ IMPORTANT: The submitted code is missing a namespace declaration. The expected namespace for this task is `{task.Namespace}`. You MUST include constructive advice about adding `namespace {task.Namespace};` at the top of their file.")
            : string.Empty;

        var compilationSection = BuildCompilationSection(executionResult, isHungarian);
        var testSection = BuildTestSection(executionResult, isHungarian);

        var userMessage = isHungarian
            ? $"""
                Feladat: {task.Title}
                Leírás: {task.Description}

                Beküldött kód:
                ```csharp
                {submission.SourceCode}
                ```

                {compilationSection}
                {testSection}
                {namespaceWarning}
                Kérlek, adj Sokratikus visszajelzést a fenti eredmények alapján!
                """
            : $"""
                Task: {task.Title}
                Description: {task.Description}

                Code Submission:
                ```csharp
                {submission.SourceCode}
                ```

                {compilationSection}
                {testSection}
                {namespaceWarning}
                Please provide Socratic feedback based on these results.
                """;

        var chatMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userMessage)
        };

        var response = await _chatClient.GetResponseAsync(chatMessages, cancellationToken: ct);
        var aiRemarks = response.Text ?? (isHungarian ? "Sajnos nem tudtam értékelni a megoldást." : "I couldn't generate a review.");

        return new Feedback
        {
            IsSuccess = isSuccess,
            Summary = executionResult.CompilationSucceeded
                ? (isHungarian ? "A futtatás befejeződött." : "Execution completed.")
                : (isHungarian ? "A fordítás sikertelen." : "Compilation failed."),
            CompilationMessages = executionResult.CompilationErrors,
            TestMessages = executionResult.Tests.Select(t => $"{t.Name}: {(t.Passed ? "Passed" : "Failed")} {t.Message}").ToList(),
            AiReviewRemarks = aiRemarks
        };
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static string BuildCompilationSection(ExecutionResult result, bool isHungarian)
    {
        if (result.CompilationSucceeded)
        {
            return isHungarian
                ? "**Fordítás:** ✅ Sikeres"
                : "**Compilation:** ✅ Succeeded";
        }

        var sb = new StringBuilder();
        sb.AppendLine(isHungarian
            ? "**Fordítás:** ❌ Sikertelen"
            : "**Compilation:** ❌ Failed");
        sb.AppendLine();
        sb.AppendLine(isHungarian ? "Fordítási hibák:" : "Compiler errors:");

        foreach (var error in result.CompilationErrors)
        {
            var csCode = ExtractCsErrorCode(error);
            if (csCode is not null)
            {
                sb.AppendLine(isHungarian
                    ? $"- `{csCode}` — {error} (Elemezd, milyen .NET szabályt sért ez a hiba!)"
                    : $"- `{csCode}` — {error} (Think about which .NET rule or concept this error code represents.)");
            }
            else
            {
                sb.AppendLine($"- {error}");
            }
        }

        if (result.RuntimeErrors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(isHungarian ? "Futásidejű hibák:" : "Runtime errors:");
            foreach (var re in result.RuntimeErrors)
                sb.AppendLine($"- {re}");
        }

        return sb.ToString();
    }

    private static string BuildTestSection(ExecutionResult result, bool isHungarian)
    {
        if (!result.CompilationSucceeded || result.Tests.Count == 0)
            return string.Empty;

        var passed = result.Tests.Count(t => t.Passed);
        var total = result.Tests.Count;

        var sb = new StringBuilder();
        sb.AppendLine(isHungarian
            ? $"**Tesztek:** {passed}/{total} sikeres"
            : $"**Tests:** {passed}/{total} passed");

        foreach (var t in result.Tests)
        {
            var icon = t.Passed ? "✅" : "❌";
            var msg = string.IsNullOrWhiteSpace(t.Message) ? string.Empty : $" — {t.Message}";
            sb.AppendLine($"{icon} `{t.Name}`{msg}");
        }

        return sb.ToString();
    }

    private static readonly Regex _csErrorCodeRegex = new(@"\bCS\d{4}\b", RegexOptions.Compiled);

    private static string? ExtractCsErrorCode(string errorMessage)
    {
        var match = _csErrorCodeRegex.Match(errorMessage);
        return match.Success ? match.Value : null;
    }
}
