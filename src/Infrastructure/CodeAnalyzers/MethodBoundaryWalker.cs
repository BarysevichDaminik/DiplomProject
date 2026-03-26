using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Infrastructure.CodeAnalyzers;

internal sealed class MethodBoundaryWalker : CSharpSyntaxWalker
{
    private List<string> currentMethodCalls = [];

    private string? currentMethodFullName;

    public List<MethodBoundary> Methods { get; } = [];

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;
        var classNode = node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        var namespaceNode = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();

        var className = classNode?.Identifier.Text ?? "Global";
        var @namespace = namespaceNode?.Name.ToString() ?? "Global";

        currentMethodFullName = $"{@namespace}.{className}.{methodName}";
        currentMethodCalls = [];

        base.VisitMethodDeclaration(node);

        var lineSpan = node.GetLocation().GetLineSpan();
        Methods.Add(new MethodBoundary(
            currentMethodFullName,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.EndLinePosition.Line + 1,
            [.. currentMethodCalls.Distinct()]));

        currentMethodFullName = null;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (currentMethodFullName != null)
        {
            var calledName = node.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax i => i.Identifier.Text,
                _ => node.Expression.ToString()
            };

            currentMethodCalls.Add(calledName);
        }

        base.VisitInvocationExpression(node);
    }
}

internal record struct MethodBoundary(
    string fullName,
    int startLine,
    int endLine,
    List<string> calls);