using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

using Application;

using Core.ReportModels;

using Infrastructure.Coverage;
using Infrastructure.Providers.GitProvider;
using Infrastructure.CodeAnalyzers;

namespace CLI;

public static partial class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Smart Test Selection System ===");

        if (args.Length < 3)
        {
            Console.WriteLine("[ERROR] Неверное количество аргументов.");
            Console.WriteLine("Использование: STSS <RepoPath> <OldSha> <NewSha> [DashboardUrl]");
            Console.WriteLine("Пример: dotnet run -- /home/user/MyProject 994552a 09f1180 http://localhost:5000/api/report");
            return;
        }

        var repoPath = args[0];
        var oldSha = args[1];
        var newSha = args[2];

        string dashboardUrl = args.Length >= 4 ? args[3] : "http://localhost:5000/api/report";

        Console.WriteLine($"[INFO] Репозиторий: {repoPath}");
        Console.WriteLine($"[INFO] Анализ коммитов: {oldSha} -> {newSha}");

        var coverageFile = Directory.GetFiles(repoPath, "coverage.cobertura.xml", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();

        if (coverageFile == null)
        {
            Console.WriteLine("[INFO] База знаний (coverage.xml) не найдена.");
            Console.WriteLine("[INFO] Запускаю режим 'Cold Start' (Полный прогон для сбора данных)...");

            int totalTests = CountAllTestsInProject(repoPath);
            var testResult = RunDotnetTest(repoPath, "--collect:\"XPlat Code Coverage\"");

            var coldReport = new EfficiencyReport
            {
                RunDate = DateTime.Now,
                TotalTestsTime = Math.Round(testResult.TimeInSeconds, 2),
                TestsRunCount = testResult.TestsExecuted,
                TotalTestsCount = totalTests,
                ImpactedMethods = [new Core.GitModels.CodeEntity
                {
                    FullName = "Full Regression Run (Cold Start)",
                    Path = "System",
                    Type = Core.GitModels.EntityType.Method,
                    ImpactReason = "Direct",
                    PropagationLevel = 0
                }

                ],
                TimeSaved = 0.0
            };

            var coldJsonReport = JsonSerializer.Serialize(coldReport, JsonOptions);
            try
            {
                using var client = new HttpClient();
                var content = new StringContent(coldJsonReport, System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(dashboardUrl, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Дашборд не обновлен (возможно, сервер выключен): {ex.Message}");
            }

            Console.WriteLine($"\n[SUCCESS] База знаний создана за {testResult.TimeInSeconds:F2} сек! Запустите систему еще раз для умной селекции.");
            return;
        }

        var gitProvider = new LibGitProvider(repoPath);
        var gitResult = await gitProvider.GetChangesBetweenCommitsAsync(oldSha, newSha);
        if (!gitResult.IsSuccess || gitResult.Result == null)
        {
            Console.WriteLine($"[ERROR] Ошибка Git: {gitResult.Message}");
            return;
        }

        var coverageMap = CoverletParser.Parse(coverageFile);
        var impactedTests = TestSelector.SelectTests(gitResult.Result, coverageMap, repoPath);
        int totalTestsInProject = CountAllTestsInProject(repoPath);

        double actualRunTime = 0;
        int actualTestsRun = 0;

        if (impactedTests.Count > 0)
        {
            var filter = (impactedTests.Contains("AllTests") || impactedTests.Contains("*"))
                ? string.Empty : $"--filter \"{string.Join("|", impactedTests)}\"";

            var testResult = RunDotnetTest(repoPath, filter);
            actualRunTime = testResult.TimeInSeconds;
            actualTestsRun = testResult.TestsExecuted;
        }

        var graph = Infrastructure.GraphCreation.GraphBuilder.Build(repoPath);
        var impactedEntities = new List<Core.GitModels.CodeEntity>();

        foreach (var file in gitResult.Result)
        {
            foreach (var m in RozlynCodeAnalyzer.GetAffectedMethods(file, repoPath))
            {
                impactedEntities.Add(m with { ImpactReason = "Direct", PropagationLevel = 0 });
                foreach (var ancestor in graph.GetAncestors(m.FullName))
                {
                    impactedEntities.Add(new Core.GitModels.CodeEntity
                    {
                        FullName = ancestor.Key,
                        Path = "Inferred via Graph",
                        Type = Core.GitModels.EntityType.Method,
                        ImpactReason = "Inferred",
                        PropagationLevel = ancestor.Value
                    });
                }
            }
        }

        var uniqueEntities = impactedEntities
            .GroupBy(e => e.FullName)
            .Select(g => g.OrderBy(e => e.PropagationLevel).First())
            .ToList();

        int skippedTests = totalTestsInProject - actualTestsRun;
        double simulatedFullTime = actualRunTime + (skippedTests * 0.5);

        double timeSavedPercent = 100.0;
        if (simulatedFullTime > 0)
        {
            timeSavedPercent = Math.Max(0, (simulatedFullTime - actualRunTime) / simulatedFullTime * 100);
        }

        var report = new EfficiencyReport
        {
            RunDate = DateTime.Now,
            TotalTestsTime = Math.Round(actualRunTime, 2),
            TestsRunCount = actualTestsRun,
            TotalTestsCount = totalTestsInProject,
            ImpactedMethods = uniqueEntities,
            TimeSaved = Math.Round(timeSavedPercent, 2),
        };

        var jsonReport = JsonSerializer.Serialize(report, JsonOptions);

        try
        {
            using var client = new HttpClient();
            var content = new StringContent(jsonReport, System.Text.Encoding.UTF8, "application/json");

            Console.WriteLine($"[INFO] Отправка данных на сервер дашборда ({dashboardUrl})...");
            var response = await client.PostAsync(dashboardUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("[SUCCESS] Отчет успешно опубликован на дашборде!");
            }
            else
            {
                Console.WriteLine($"[WARNING] Сервер вернул ошибку: {response.StatusCode}");
                await File.WriteAllTextAsync("efficiency.json", jsonReport);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("[WARNING] Сервер дашборда недоступен. Сохраняю отчет локально.");
            await File.WriteAllTextAsync("efficiency.json", jsonReport);
        }

        Console.WriteLine($"\n[SUCCESS] Анализ завершен. Экономия: {report.TimeSaved}%");
    }

    private static (double TimeInSeconds, int TestsExecuted) RunDotnetTest(string workingDir, string filter)
    {
        var timer = Stopwatch.StartNew();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.IsNullOrEmpty(filter) ? "test" : $"test {filter}",
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo);
        string output = process!.StandardOutput.ReadToEnd();
        process.WaitForExit();
        timer.Stop();

        Console.WriteLine(output);

        int executedCount = 0;
        var match = MyRegex().Match(output);
        if (match.Success)
        {
            executedCount = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        return (timer.Elapsed.TotalSeconds, executedCount);
    }

    private static int CountAllTestsInProject(string rootPath)
    {
        int count = 0;
        var testFiles = Directory.GetFiles(rootPath, "*Tests.cs", SearchOption.AllDirectories);
        foreach (var file in testFiles)
        {
            var code = File.ReadAllText(file);
            count += code.Split("[Fact]").Length - 1;
            count += code.Split("[Theory]").Length - 1;
        }

        return count == 0 ? 1 : count;
    }

    [GeneratedRegex(@"(?i)total:\s*(\d+)")]
    private static partial Regex MyRegex();
}