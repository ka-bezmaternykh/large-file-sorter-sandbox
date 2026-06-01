using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services.Tests;

public class BinaryRowFormatterTests
{
    [Fact]
    public void TryFormat_ShouldThrowNotImplementedException()
    {
        var formatter = new BinaryRowFormatter();
        var item = new Item(1, "Apple"u8.ToArray());
        var buffer = new byte[32];

        Assert.Throws<NotImplementedException>(() => formatter.TryFormat(item, buffer, out _));
    }
}
