using System.Globalization;
using System.Text;

using Infrastructure.Providers.GitProvider;

namespace Infrastructure.Tests;

public class GitTest
{
    [Fact]
    public async Task GetChangesBetweenCommits_ShouldShowDifference()
    {
        // arrange
        var repoPath = @"/home/noone/Documents/projects/MyProject";
        var provider = new LibGitProvider(repoPath);

        string firstCommitSha = "58e9b4c0f9eae0c5e30ed17a49079e18aad99d01";
        string secondCommitSha = "931bd4561ae4a6f699cde7928ef90e62e6354341";

        // act
        var response = await provider.GetChangesBetweenCommitsAsync(firstCommitSha, secondCommitSha);

        // assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccess, $"Метод вернул ошибку: {response.Message}");
        Assert.NotNull(response.Result);
        Assert.NotEmpty(response.Result);

        // output
        var changedFiles = response.Result;
        int i = 0;
        foreach (var file in changedFiles)
        {
            Console.WriteLine($"Изменен в истории: {file.NewPath}");
            StringBuilder sb = new();
            for (int b = 0; b < file.ChangedLineRanges.Count; b++)
            {
                var lineRange = file.ChangedLineRanges[b];
                sb.AppendLine(lineRange.Start.ToString(CultureInfo.InvariantCulture) + "x" + lineRange.Count);
            }

            Console.WriteLine($"диапазон для данного файла: {sb}\n");
            i++;
        }

        Console.WriteLine($"total {i} files");
    }
}