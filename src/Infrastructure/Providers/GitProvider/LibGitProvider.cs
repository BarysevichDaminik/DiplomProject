using Core.Results;
using Core.GitModels;

using LibGit2Sharp;

using Infrastructure.Mappers;

namespace Infrastructure.Providers.GitProvider;

public sealed class LibGitProvider(string path) : IGitProvider
{
    public async Task<IResult<List<ChangedFile>>> GetChangesBetweenCommitsAsync(string oldSha, string newSha)
    {
        return await Task.Run<IResult<List<ChangedFile>>>(() =>
        {
            using var repo = new Repository(path);

            if (string.IsNullOrEmpty(path))
            {
                return new EmptyPathResult<List<ChangedFile>>(default);
            }

            var oldTree = repo.Lookup<Commit>(oldSha).Tree;
            var newTree = repo.Lookup<Commit>(newSha).Tree;

            var patch = repo.Diff.Compare<Patch>(oldTree, newTree);

            List<ChangedFile> result = [];

            foreach (var entry in patch)
            {
                if (System.IO.Path.GetExtension(entry.Path) != ".cs")
                {
                    continue;
                }

                var ranges = entry.AddedLines
                    .Select(l => l.LineNumber)
                    .OrderBy(n => n)
                    .Aggregate(new List<LineRange>(), (acc, line) =>
                    {
                        if (acc.Count > 0 && acc[^1].Start + acc[^1].Count == line)
                        {
                            var last = acc[^1];
                            acc[^1] = last with { Count = last.Count + 1 };
                        }
                        else
                        {
                            acc.Add(new LineRange { Start = line, Count = 1 });
                        }

                        return acc;
                    });

                var changedFile = new ChangedFile
                {
                    OldPath = entry.OldPath,
                    NewPath = entry.Path,
                    ChangeType = StatusMapper.ConvertType(entry.Status),
                    ChangedLineRanges = ranges,
                };

                result.Add(changedFile);
            }

            return new SuccessResult<List<ChangedFile>>(result);
        });
    }
}