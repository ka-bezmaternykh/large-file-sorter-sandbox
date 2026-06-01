using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services;

public sealed class ItemComparer : IComparer<Item>
{
    public int Compare(Item x, Item y)
    {
        var textComparison = x.TextBytes.AsSpan().SequenceCompareTo(y.TextBytes);
        if (textComparison != 0)
        {
            return textComparison;
        }

        return x.Number.CompareTo(y.Number);
    }
}
