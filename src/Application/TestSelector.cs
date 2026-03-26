using Core.GitModels;
using Core.TestModels;

using Infrastructure.CodeAnalyzers;
using Infrastructure.GraphCreation;

namespace Application;

public static class TestSelector
{
    public static List<string> SelectTests(List<ChangedFile> changedFiles, TestToCodeMap coverageMap, string rootPath)
    {
        var impactedEntities = new HashSet<CodeEntity>();
        var smartFilters = new HashSet<string>();

        bool hasDeletedFiles = changedFiles.Any(f => f.ChangeType == ChangeTypes.Deleted);
        if (hasDeletedFiles)
        {
            Console.WriteLine("[SAFE MODE] Обнаружено удаление файлов исходного кода. Анализ графа невозможен. Запуск полного прогона.");
            return ["*"];
        }

        changedFiles.ForEach(file =>
        {
            if (file.NewPath.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase))
            {
                var testClassName = Path.GetFileNameWithoutExtension(file.NewPath);
                smartFilters.Add(testClassName);
            }
        });

        var graph = GraphBuilder.Build(rootPath);

        foreach (var file in changedFiles)
        {
            if (file.NewPath.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase))
            {
                var testClassName = Path.GetFileNameWithoutExtension(file.NewPath);
                smartFilters.Add(testClassName);
                Console.WriteLine($"[INFO] Обнаружен новый или измененный тестовый файл: {testClassName}. Принудительно добавлен в очередь.");
                continue;
            }

            var methods = RozlynCodeAnalyzer.GetAffectedMethods(file, rootPath);
            foreach (var m in methods)
            {
                impactedEntities.Add(m with { ImpactReason = "Direct", PropagationLevel = 0 });

                var ancestors = graph.GetAncestors(m.FullName);
                foreach (var ancestor in ancestors)
                {
                    impactedEntities.Add(new CodeEntity
                    {
                        FullName = ancestor.Key,
                        Path = "Inferred via Graph",
                        Type = EntityType.Method,
                        ImpactReason = "Inferred",
                        PropagationLevel = ancestor.Value
                    });
                }
            }
        }

        var impactedNames = impactedEntities.Select(e => e.FullName).ToHashSet();

        bool isAnyImpactedMethodCovered = coverageMap.Map.Values
            .Any(methodsInMap => methodsInMap.Any(m => impactedNames.Contains(m)));

        if (!isAnyImpactedMethodCovered && changedFiles.Count > 0 && smartFilters.Count == 0)
        {
            return ["*"];
        }

        var keywords = impactedEntities
            .Select(e => e.FullName.Split('.').Reverse().Skip(1).FirstOrDefault())
            .Where(name => name != null)
            .Select(name => name!.Replace("Service", string.Empty).Replace("Controller", string.Empty))
            .Distinct();

        foreach (var kw in keywords)
        {
            smartFilters.Add(kw!);
        }

        if (smartFilters.Count > 10)
        {
            Console.WriteLine("[SAFE MODE] Обнаружен глобальный рефакторинг. Запуск полного регрессионного тестирования.");
            return ["*"];
        }

        return smartFilters.Count == 0 && changedFiles.Count > 0 ? ["*"] : [.. smartFilters];
    }
}