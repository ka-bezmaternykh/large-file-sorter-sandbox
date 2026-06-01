using System.Buffers.Text;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services;

public sealed class TextRowFormatter : IItemFormatter
{
    public bool TryFormat(Item item, Span<byte> destination, out int written)
    {
        var requiredLength = CountDigits(item.Number) + 3 + item.TextBytes.Length;
        if (destination.Length < requiredLength)
        {
            written = 0;
            return false;
        }

        if (!Utf8Formatter.TryFormat(item.Number, destination, out var digitsWritten))
        {
            written = 0;
            return false;
        }

        destination[digitsWritten] = (byte)'.';
        destination[digitsWritten + 1] = (byte)' ';
        item.TextBytes.AsSpan().CopyTo(destination[(digitsWritten + 2)..]);
        destination[requiredLength - 1] = (byte)'\n';
        written = requiredLength;
        return true;
    }

    private static int CountDigits(long value)
    {
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }
}
