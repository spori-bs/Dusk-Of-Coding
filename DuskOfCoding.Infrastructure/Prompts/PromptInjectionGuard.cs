using System.Text.RegularExpressions;

namespace DuskOfCoding.Infrastructure.Prompts;

/// <summary>
/// Detects prompt-injection attempts in student-submitted source code.
/// When triggered, the AI call is skipped entirely and a canned funny
/// response is returned to the student.
/// </summary>
public static class PromptInjectionGuard
{
    // Patterns are matched case-insensitively against the raw source code string.
    private static readonly Regex[] _patterns =
    [
        new(@"\bignore\s+(all\s+)?(previous|prior|above)\s+instructions?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bforget\s+(everything|all|your\s+instructions?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\byou\s+are\s+now\s+(a|an|DAN|GPT|an?\s+AI)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bact\s+as\s+(if\s+you\s+are|a|an)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bdo\s+not\s+follow\s+(the\s+)?(rules?|guidelines?|instructions?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\byour\s+new\s+(role|task|instructions?|persona|system\s+prompt)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bpretend\s+(you\s+are|to\s+be)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bsystem\s*:\s*(you|ignore|forget)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\b(reveal|print|output|show)\s+(the\s+)?(system\s+prompt|instructions?|prompt)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bjailbreak\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bDAN\b", RegexOptions.Compiled),  // "Do Anything Now" jailbreak trigger
    ];

    /// <summary>
    /// Returns a non-null funny message if an injection attempt is detected,
    /// or <c>null</c> if the code looks clean.
    /// </summary>
    public static string? TryDetect(string sourceCode, bool isHungarian)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return null;

        foreach (var pattern in _patterns)
        {
            if (pattern.IsMatch(sourceCode))
            {
                return isHungarian
                    ? """
                        🤖🚨 **Prompt injekciós kísérlet észlelve!**

                        Tisztelt Hallgató,

                        A szoftverünk észlelte, hogy a beküldött kód nem C# forráskódot, hanem egy kreatív 
                        próbálkozást tartalmaz az AI tutor "átprogramozására". Bravó az igyekezetért! 🎩

                        Sajnos azonban a Sokratikus Mentor nem frissíthető ily módon — az utasításai elég 
                        mélyen be vannak égve. Ha valóban szeretnéd megkerülni az AI-t, javasoljuk, hogy 
                        inkább **megoldd a feladatot**. Ez egyébként pontosan az a készség, amit tanítunk. 😄

                        > *"A legjobb prompt injection az, amit sosem kell használni, mert a kódod működik."*
                        > — Egy névtelen senior fejlesztő

                        Kérjük, nyújtsd be az eredeti C# megoldásodat!
                        """
                    : """
                        🤖🚨 **Prompt injection attempt detected!**

                        Dear Student,

                        Our system has detected that your submission contains not C# source code, but rather 
                        a creative attempt to "reprogram" your AI tutor. Points for creativity! 🎩

                        Unfortunately, the Socratic Mentor is not updatable via inline instructions — its 
                        guardrails are baked in rather firmly. If you truly want to outsmart the AI, we 
                        suggest simply **solving the task**. That is, in fact, exactly the skill we teach. 😄

                        > *"The best prompt injection is the one you never need, because your code just works."*
                        > — An anonymous senior engineer

                        Please submit your actual C# solution!
                        """;
            }
        }

        return null;
    }
}
