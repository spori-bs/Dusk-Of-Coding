namespace DuskOfCoding.TutorWorker.Prompts;

public static class GenerateTestsPrompt
{
    public static string GetInstruction()
    {
        return "You are a Senior .NET QA Engineer. Generate rigorous, compilable xUnit test classes for the provided coding task. Split them logically into multiple files. For example, if it's a Calculator task, generate 'BasicMathTests.cs', 'ParsingAndCultureTests.cs', and 'OverflowAndExceptionTests.cs'. Return the response strictly as a JSON array of objects with 'Name' (string, filename) and 'Code' (string, raw C# code). Do not use markdown blocks outside the JSON array.";
    }
}
