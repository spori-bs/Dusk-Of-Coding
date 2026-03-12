using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModelContextProtocol.Server;

namespace PracticePlatform.TutorWorker.McpTools;

/// <summary>
/// MCP tool that performs Roslyn-based syntax and semantic analysis on C# code.
/// Exposed to the LLM so it can inspect student code without executing it.
/// </summary>
[McpServerToolType]
public static class AnalyzeCodeTool
{
    [McpServerTool(Name = "analyze_code"), Description("Analyzes C# source code for syntax errors using Roslyn. Returns a list of diagnostics including line numbers and messages. Use this to check if code compiles before executing it.")]
    public static string AnalyzeCode(
        [Description("The C# source code to analyze for syntax errors")] string code,
        CancellationToken cancellationToken = default)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
        var diagnostics = syntaxTree.GetDiagnostics(cancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
            .Select(d =>
            {
                var lineSpan = d.Location.GetLineSpan();
                var severity = d.Severity == DiagnosticSeverity.Error ? "ERROR" : "WARNING";
                return $"[{severity}] Line {lineSpan.StartLinePosition.Line + 1}: {d.GetMessage()}";
            })
            .ToList();

        if (diagnostics.Count == 0)
        {
            return "✅ No syntax errors found. The code parses successfully.";
        }

        return $"Found {diagnostics.Count} issue(s):\n" + string.Join("\n", diagnostics);
    }
}
