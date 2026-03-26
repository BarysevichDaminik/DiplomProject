using Infrastructure.Providers.GitProvider;
using Infrastructure.CodeAnalyzers;

namespace Infrastructure.Tests;

public class IntegrationImpactTest
{
    [Fact]
    public async Task Should_Find_Affected_Methods_Between_Commits()
    {
        // Arrange
        var repoPath = @"/home/noone/Documents/projects/MyProject";
        var gitProvider = new LibGitProvider(repoPath);

        string oldSha = "58e9b4c0f9eae0c5e30ed17a49079e18aad99d01";
        string newSha = "931bd4561ae4a6f699cde7928ef90e62e6354341";

        // Act
        var gitResponse = await gitProvider.GetChangesBetweenCommitsAsync(oldSha, newSha);

        Assert.True(gitResponse.IsSuccess);
        var changedFiles = gitResponse.Result;

        Console.WriteLine($"Found modified files: {changedFiles!.Count}");

        var allAffectedMethods = new List<Core.GitModels.CodeEntity>();

        foreach (var file in changedFiles)
        {
            var methods = RozlynCodeAnalyzer.GetAffectedMethods(file, repoPath);
            allAffectedMethods.AddRange(methods);
        }

        // 3. Assert & Output
        foreach (var method in allAffectedMethods)
        {
            Console.WriteLine($"[IMPACT] Метод: {method.FullName}");
            Console.WriteLine($"          Файл: {method.Path}");
        }

        Assert.NotEmpty(allAffectedMethods);
    }
}