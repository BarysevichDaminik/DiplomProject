using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Core.GitModels;

namespace Infrastructure.CodeAnalyzers;

public static class RozlynCodeAnalyzer
{
    public static List<CodeEntity> GetAffectedMethods(ChangedFile changedFile, string rootPath)
    {
        var fullPath = Path.Combine(rootPath, changedFile.NewPath);
        if (!File.Exists(fullPath))
        {
            return [];
        }

        var code = File.ReadAllText(fullPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var walker = new MethodBoundaryWalker();
        walker.Visit(root);

        var affectedEntities = new List<CodeEntity>();

        affectedEntities.AddRange(walker.Methods
                        .Where(method => changedFile.ChangedLineRanges.Any(r => Intersects(method, r)))
                        .Select(method => new CodeEntity
                        {
                            FullName = method.fullName,
                            Path = changedFile.NewPath,
                            Type = EntityType.Method,
                        }));

        return affectedEntities;
    }

    private static bool Intersects(MethodBoundary method, LineRange range)
    {
        return range.Start <= method.endLine && (range.Start + range.Count) >= method.startLine;
    }
}