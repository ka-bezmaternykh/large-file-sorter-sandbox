using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services.Tests;

public class ItemComparerTests
{
    [Fact]
    public void Compare_ShouldOrderByTextThenByNumber()
    {
        var comparer = new ItemComparer();
        var items = new List<Item>
        {
            new(3, "Banana"u8.ToArray()),
            new(7, "Apple"u8.ToArray()),
            new(2, "Apple"u8.ToArray())
        };

        items.Sort(comparer);

        Assert.Equal(2, items[0].Number);
        Assert.Equal("Apple"u8.ToArray(), items[0].TextBytes);
        Assert.Equal(7, items[1].Number);
        Assert.Equal("Apple"u8.ToArray(), items[1].TextBytes);
        Assert.Equal(3, items[2].Number);
        Assert.Equal("Banana"u8.ToArray(), items[2].TextBytes);
    }
}
