using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DuskOfCoding.ExecutionApi.Services;

public class SecureCompilationService
{
    private readonly IEnumerable<MetadataReference> _references;

    public SecureCompilationService()
    {
        // Load trusted references
        var trustedAssemblies = new[]
        {
            typeof(object).Assembly.Location, // mscorlib / System.Private.CoreLib
            typeof(Console).Assembly.Location, // System.Console
            typeof(System.Collections.IEnumerable).Assembly.Location, // System.Collections
            typeof(System.Linq.Enumerable).Assembly.Location, // System.Linq
            typeof(System.Text.StringBuilder).Assembly.Location, // System.Text
            typeof(System.Collections.Generic.List<>).Assembly.Location, // System.Collections.Generic
            // Need runtime
            System.Reflection.Assembly.Load("System.Runtime").Location,
            // xUnit
            typeof(Xunit.FactAttribute).Assembly.Location, // xunit.core
            typeof(Xunit.Assert).Assembly.Location // xunit.assert
        };

        _references = trustedAssemblies.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    }

    public (bool IsValid, List<string> Errors, CSharpCompilation? Compilation) Compile(string studentCode, string[] testBundleCode)
    {
        var studentTree = CSharpSyntaxTree.ParseText(studentCode);
        
        // 1. Analyze for security violations
        var analyzer = new SandboxSyntaxAnalyzer();
        analyzer.Visit(studentTree.GetRoot());
        
        if (analyzer.Errors.Any())
        {
            return (false, analyzer.Errors, null);
        }

        // 2. Rewrite for safety (Loops & Recursion)
        var rewriter = new SafetyRewriter();
        var rewrittenRoot = rewriter.Visit(studentTree.GetRoot());
        
        // Inject fields into the first class or globally
        // For simplicity, we can inject a global static class that holds the CTS and depth
        var injection = CSharpSyntaxTree.ParseText(@"
public static class __SandboxState 
{
    [System.ThreadStatic] public static int __callDepth;
    public static System.Threading.CancellationToken __cts;
}").GetRoot();

        var safeStudentTree = studentTree.WithRootAndOptions(rewrittenRoot, studentTree.Options);
        var testTrees = testBundleCode.Select(testCode=> CSharpSyntaxTree.ParseText( testCode ));
        var injectedTree = CSharpSyntaxTree.ParseText(injection.ToFullString());
        
        var compilationTrees = new List<SyntaxTree> { safeStudentTree, injectedTree }
        .Concat( testTrees );
        
        // 3. Compile
        var compilation = CSharpCompilation.Create(
            $"Submission_{Guid.NewGuid()}",
            compilationTrees,
            _references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
            )
        );

        var emitDiagnostics = compilation.GetDiagnostics();
        var errors = emitDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}")
            .ToList();

        if (errors.Any())
        {
            return (false, errors, null);
        }

        return (true, new List<string>(), compilation);
    }
}

public class SandboxSyntaxAnalyzer : CSharpSyntaxWalker
{
    public List<string> Errors { get; } = new();

    public override void VisitUnsafeStatement(UnsafeStatementSyntax node) => ReportAndBase(node, "Unsafe blocks are not allowed.");
    public override void VisitPointerType(PointerTypeSyntax node) => ReportAndBase(node, "Pointer types are not allowed.");
    public override void VisitPragmaWarningDirectiveTrivia(PragmaWarningDirectiveTriviaSyntax node) => ReportAndBase(node, "#pragma directives are not allowed.");
    public override void VisitAttributeList(AttributeListSyntax node)
    {
        foreach (var attr in node.Attributes)
        {
            var name = attr.Name.ToString();
            if (name.Contains("DllImport") || name.Contains("LibraryImport"))
                Errors.Add("P/Invoke (DllImport/LibraryImport) is not allowed.");
        }
        base.VisitAttributeList(node);
    }
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.ExternKeyword)))
            Errors.Add("extern methods are not allowed.");
        base.VisitMethodDeclaration(node);
    }
    public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
    {
        if (node.Alias.Identifier.Text == "global")
            Errors.Add("global:: alias qualification is not allowed.");
        base.VisitAliasQualifiedName(node);
    }
    public override void VisitGotoStatement(GotoStatementSyntax node) => ReportAndBase(node, "goto statements are not allowed.");
    
    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        if (node.Declaration == null || 
            node.Declaration.Type.ToString() == "Exception" || 
            node.Declaration.Type.ToString() == "System.Exception" ||
            node.Declaration.Type.ToString() == "SystemException" ||
            node.Declaration.Type.ToString() == "System.SystemException")
        {
            Errors.Add("Broad exception catching (catch, catch(Exception)) is not allowed. Catch specific exceptions instead.");
        }
        base.VisitCatchClause(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var methodAsString = node.Expression.ToString();
        if (methodAsString == "GetType" || methodAsString.EndsWith(".GetType"))
            Errors.Add("GetType() is not allowed.");
            
        base.VisitInvocationExpression(node);
    }
    
    public override void VisitTypeOfExpression(TypeOfExpressionSyntax node) => ReportAndBase(node, "typeof() is not allowed.");
    
    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (node.Name?.ToString().Contains("System.Reflection") == true)
            Errors.Add("System.Reflection is not allowed.");
        base.VisitUsingDirective(node);
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var name = node.Identifier.Text;
        if (name == "Activator" || name == "Type" || name == "Assembly")
            Errors.Add($"Reflection type '{name}' is not allowed.");
        base.VisitIdentifierName(node);
    }

    private void ReportAndBase(SyntaxNode node, string message)
    {
        Errors.Add(message);
        base.Visit(node);
    }
}

public class SafetyRewriter : CSharpSyntaxRewriter
{
    private static readonly StatementSyntax CtsCheck = SyntaxFactory.ParseStatement("__SandboxState.__cts.ThrowIfCancellationRequested();\n");

    private BlockSyntax InjectIntoBlock(BlockSyntax? block)
    {
        if (block == null) return SyntaxFactory.Block(CtsCheck);
        return block.WithStatements(block.Statements.Insert(0, CtsCheck));
    }

    private StatementSyntax InjectIntoStatement(StatementSyntax statement)
    {
        if (statement is BlockSyntax block) return InjectIntoBlock(block);
        return SyntaxFactory.Block(CtsCheck, statement);
    }

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node) =>
        base.VisitWhileStatement(node.WithStatement(InjectIntoStatement(node.Statement)));

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node) =>
        base.VisitForStatement(node.WithStatement(InjectIntoStatement(node.Statement)));

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node) =>
        base.VisitForEachStatement(node.WithStatement(InjectIntoStatement(node.Statement)));

    public override SyntaxNode? VisitDoStatement(DoStatementSyntax node) =>
        base.VisitDoStatement(node.WithStatement(InjectIntoStatement(node.Statement)));

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var baseNode = (MethodDeclarationSyntax?)base.VisitMethodDeclaration(node);
        if (baseNode?.Body == null) return baseNode;

        var statements = baseNode.Body.Statements;
        var newBodyStr = @"
{
    if (++__SandboxState.__callDepth > 200) throw new System.InvalidOperationException(""Maximum recursion depth exceeded."");
    try
    {
        " + string.Join("\n", statements.Select(s => s.ToFullString())) + @"
    }
    finally
    {
        __SandboxState.__callDepth--;
    }
}";
        var newBody = (BlockSyntax)SyntaxFactory.ParseStatement(newBodyStr);
        return baseNode.WithBody(newBody);
    }
}
