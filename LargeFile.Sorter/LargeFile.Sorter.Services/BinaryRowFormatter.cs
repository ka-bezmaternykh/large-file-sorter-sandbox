using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services;

public sealed class BinaryRowFormatter : IItemFormatter
{
    public bool TryFormat(Item item, Span<byte> destination, out int written)
    {
        throw new NotImplementedException();
    }
}
