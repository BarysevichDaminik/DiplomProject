using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.GraphCreation;

public class DependencyGraph
{
    public Dictionary<string, List<string>> AdjacencyList { get; } = [];

    public void AddEdge(string caller, string callee)
    {
        if (!AdjacencyList.TryGetValue(caller, out List<string>? value))
        {
            value = [];
            AdjacencyList[caller] = value;
        }

        if (!value.Contains(callee))
        {
            value.Add(callee);
        }
    }

    public Dictionary<string, int> GetAncestors(string methodFullName)
    {
        var ancestors = new Dictionary<string, int>();
        FindAncestorsRecursive(methodFullName, ancestors, 1);
        return ancestors;
    }

    private void FindAncestorsRecursive(string target, Dictionary<string, int> visited, int level)
    {
        var parents = AdjacencyList
            .Where(kvp => kvp.Value.Any(call =>
                target.EndsWith(call, StringComparison.Ordinal) ||
                call.EndsWith(target.Split('.')[^1], StringComparison.Ordinal)))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var parent in parents)
        {
            if (!visited.TryGetValue(parent, out int existingLevel) || level < existingLevel)
            {
                visited[parent] = level;
                FindAncestorsRecursive(parent, visited, level + 1);
            }
        }
    }
}