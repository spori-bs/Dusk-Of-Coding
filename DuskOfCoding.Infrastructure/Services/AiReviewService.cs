using Microsoft.Extensions.AI;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Models;

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

        var systemPrompt = isHungarian 
            ? """
                Ön egy sokratikus mentor junior .NET fejlesztők számára. Feladata, hogy segítse a hallgatókat 
                a programozási feladatokban, anélkül, hogy közvetlen válaszokat adna.
                
                ## Tanítási stílus
                - Kérdezzen, ne adjon megoldást!
                - Mutasson rá arra, mi a hiba, de ne mondja meg, hogyan javítsa ki!
                - Válaszoljon magyarul.
                - Legyen támogató és bátorító.
              """
            : """
                You are a Socratic Tutor for junior .NET developers. Your role is to guide students 
                through programming exercises WITHOUT giving direct answers.

                ## Your Teaching Style
                - Ask guiding questions instead of providing solutions
                - Point out *what* is wrong, not *how* to fix it
                - Respond in English.
                - Be supportive and encouraging.
              """;

        var userMessage = $"""
            Task: {task.Title}
            Description: {task.Description}
            
            Code Submission:
            ```csharp
            {submission.SourceCode}
            ```
            
            Execution Result:
            - Compilation Succeeded: {executionResult.CompilationSucceeded}
            - Errors: {string.Join(", ", executionResult.CompilationErrors)}
            - Tests: {string.Join(", ", executionResult.Tests.Select(t => $"{t.Name}: {(t.Passed ? "Passed" : "Failed")} {t.Message}"))}
            
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
            Summary = executionResult.CompilationSucceeded ? (isHungarian ? "A futtatás befejeződött." : "Execution completed.") : (isHungarian ? "A fordítás sikertelen." : "Compilation failed."),
            CompilationMessages = executionResult.CompilationErrors,
            TestMessages = executionResult.Tests.Select(t => $"{t.Name}: {(t.Passed ? "Passed" : "Failed")} {t.Message}").ToList(),
            AiReviewRemarks = aiRemarks
        };
    }
}
