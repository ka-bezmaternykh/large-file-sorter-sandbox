using System.Text;
using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services.Tests;

public class TextRowFormatterTests
{
    [Fact]
    public void TryFormat_ShouldWriteTextRow()
    {
        var formatter = new TextRowFormatter();
        var item = new Item(415, "Apple"u8.ToArray());
        var buffer = new byte[64];

        var formatted = formatter.TryFormat(item, buffer, out var written);

        Assert.True(formatted);
        Assert.Equal("415. Apple\n", Encoding.UTF8.GetString(buffer, 0, written));
    }

    [Fact]
    public void TryFormat_ShouldReturnFalseWhenBufferIsTooSmall()
    {
        var formatter = new TextRowFormatter();
        var item = new Item(415, "Apple"u8.ToArray());
        var buffer = new byte[4];

        var formatted = formatter.TryFormat(item, buffer, out var written);

        Assert.False(formatted);
        Assert.Equal(0, written);
    }
}
