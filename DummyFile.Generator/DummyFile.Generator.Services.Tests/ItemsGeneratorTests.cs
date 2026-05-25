using DummyFile.Generator.Services.Abstract;
namespace DummyFile.Generator.Services.Tests;

public class ItemsGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnValidItems()
    {
        IItemsGenerator generator = new ItemsGenerator();

        var items = new List<Item>();
        for (var index = 0; index < 5; index++)
        {
            generator.Generate(out var item);
            items.Add(item);
        }

        Assert.Equal(5, items.Count);
        Assert.All(items, item =>
        {
            Assert.True(item.Number > 0);
            Assert.InRange(item.Number, 1, int.MaxValue);
            Assert.False(item.Text.IsEmpty);
            Assert.True(item.Text.Length <= 1024);
            Assert.DoesNotContain((byte)'\n', item.TextSpan.ToArray());
            Assert.DoesNotContain((byte)'\r', item.TextSpan.ToArray());
            Assert.All(item.TextSpan.ToArray(), value => Assert.InRange(value, (byte)32, (byte)126));
        });
    }

    [Fact]
    public void Generate_ShouldAdvanceNumberSequence()
    {
        IItemsGenerator generator = new ItemsGenerator();

        generator.Generate(out var first);
        generator.Generate(out var second);

        Assert.NotEqual(first.Number, second.Number);
    }

    [Fact]
    public void Generate_ShouldReturnIndependentTextBuffers()
    {
        IItemsGenerator generator = new ItemsGenerator();

        generator.Generate(out var first);
        generator.Generate(out var second);

        Assert.False(first.Text.Equals(second.Text));
    }
}
