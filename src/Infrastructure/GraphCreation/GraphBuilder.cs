using Microsoft.CodeAnalysis.CSharp;

using Infrastructure.CodeAnalyzers;

namespace Infrastructure.GraphCreation;

public static class GraphBuilder
{
    public static DependencyGraph Build(string rootPath)
    {
        var graph = new DependencyGraph();
        var files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (file.Contains("/obj/") || file.Contains("/bin/") || file.Contains("Tests.cs"))
            {
                continue;
            }

            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var walker = new MethodBoundaryWalker();
            walker.Visit(root);

            foreach (var method in walker.Methods)
            {
                foreach (var call in method.calls)
                {
                    graph.AddEdge(method.fullName, call);
                }
            }
        }

        return graph;
    }
}