namespace DuskOfCoding.TutorWorker.Prompts;

public static class GenerateTestsPrompt
{
    public static string GetInstruction(string expectedClassName)
    {
        var sutClassName = string.IsNullOrWhiteSpace(expectedClassName) ? "Solution" : expectedClassName;
        var prompt = 
        """
        You are a Senior .NET QA Engineer. Generate rigorous, compilable xUnit test classes for the provided coding task.

        CRITICAL CONSTRAINTS:
        1. System Under Test (SUT): Assume the user's submitted code will be contained in a public class named `{sutClassName}` in the global namespace. You MUST instantiate or call methods on `{sutClassName}` directly. Do NOT use the name 'Solution' unless `{sutClassName}` is exactly 'Solution'.
        2. Dependencies: Use ONLY standard .NET (System.*) libraries and vanilla `Xunit` (e.g., Assert.Equal, Assert.Throws). DO NOT use Moq, FluentAssertions, or any other 3rd-party NuGet packages.
        3. Resilience (Timeouts): You MUST prevent infinite loops. Decorate every test with a timeout. Use `[Fact(Timeout = 2000)]` or `[Theory(Timeout = 2000)]`.
        4. Completeness: Every file must include all necessary using directives (`using System;`, `using Xunit;`, etc.).
        5. File Splitting: Split tests logically into multiple files (e.g., 'BasicTests.cs', 'EdgeCaseTests.cs').

        OUTPUT FORMAT:
        Return strictly a valid JSON array of objects. Do not output any conversational text. Do not wrap the JSON in markdown code blocks (no ```json).

        [
        {{ 
            ""Name"": ""BasicTests.cs"", 
            ""Code"": ""using System;\nusing Xunit;\n\npublic class BasicTests\n{{\n    [Fact(Timeout = 2000)]\n    public void Test1()\n    {{\n        // Arrange\n        var sut = new {sutClassName}();\n        // Act & Assert\n    }}\n}}"" 
        }}
        ]
        """;
        
        return prompt.Replace("{sutClassName}", sutClassName);
    }
}
