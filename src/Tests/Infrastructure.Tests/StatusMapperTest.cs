using Core.GitModels;

using Infrastructure.Mappers;

using LibGit2Sharp;

using Xunit;

namespace Infrastructure.Tests;

public class StatusMapperTest
{
    [Theory]
    [InlineData(ChangeKind.Added, ChangeTypes.Added)]
    [InlineData(ChangeKind.Deleted, ChangeTypes.Deleted)]
    [InlineData(ChangeKind.Modified, ChangeTypes.Modified)]
    [InlineData(ChangeKind.Renamed, ChangeTypes.Renamed)]
    [InlineData(ChangeKind.Copied, ChangeTypes.Copied)]
    [InlineData(ChangeKind.TypeChanged, ChangeTypes.TypeChanged)]
    [InlineData(ChangeKind.Unmodified, ChangeTypes.Undefined)]
    public void ConvertType_ShouldMapCorrectively(ChangeKind input, ChangeTypes expected)
    {
        // Act
        var result = StatusMapper.ConvertType(input);

        // Assert
        Assert.Equal(expected, result);
    }
}