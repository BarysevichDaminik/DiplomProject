using Core.GitModels;

using LibGit2Sharp;

namespace Infrastructure.Mappers;

public static class StatusMapper
{
    public static ChangeTypes ConvertType(ChangeKind status)
    {
        return status.ToString() switch
        {
            nameof(ChangeTypes.Added) => ChangeTypes.Added,
            nameof(ChangeTypes.Deleted) => ChangeTypes.Deleted,
            nameof(ChangeTypes.Modified) => ChangeTypes.Modified,
            nameof(ChangeTypes.Renamed) => ChangeTypes.Renamed,
            nameof(ChangeTypes.Copied) => ChangeTypes.Copied,
            nameof(ChangeTypes.TypeChanged) => ChangeTypes.TypeChanged,
            _ => ChangeTypes.Undefined,
        };
    }
}