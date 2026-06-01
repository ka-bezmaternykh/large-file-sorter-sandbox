namespace LargeFile.Sorter.Services.Tests;

public class RowParserTests
{
    [Fact]
    public void TryParse_ShouldReturnParsedItemForValidRow()
    {
        var parser = new RowParser();

        var parsed = parser.TryParse("415. Apple"u8.ToArray(), out var item);

        Assert.True(parsed);
        Assert.Equal(415, item.Number);
        Assert.Equal("Apple"u8.ToArray(), item.TextBytes);
    }

    [Fact]
    public void TryParse_ShouldReturnFalseForInvalidRow()
    {
        var parser = new RowParser();

        var parsed = parser.TryParse("Invalid row"u8.ToArray(), out _);

        Assert.False(parsed);
    }
}
