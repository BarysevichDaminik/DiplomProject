using Core.Results;
using Core.GitModels;

namespace Infrastructure.Providers.GitProvider;

public interface IGitProvider
{
    Task<IResult<List<ChangedFile>>> GetChangesBetweenCommitsAsync(string oldSha, string newSha);
}