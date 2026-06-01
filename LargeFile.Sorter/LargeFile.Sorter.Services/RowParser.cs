using System.Buffers.Text;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services;

public sealed class RowParser : IRowParser
{
    public bool TryParse(ReadOnlySpan<byte> row, out Item item)
    {
        item = default;

        if (row.IsEmpty)
        {
            return false;
        }

        var separatorIndex = FindSeparatorIndex(row);
        if (separatorIndex <= 0)
        {
            return false;
        }

        var numberBytes = row[..separatorIndex];
        if (!Utf8Parser.TryParse(numberBytes, out long number, out var consumed) ||
            consumed != numberBytes.Length ||
            number <= 0)
        {
            return false;
        }

        // deliberate allocation, since item will outlive the buffer
        var textBytes = row[(separatorIndex + 2)..].ToArray();
        item = new Item(number, textBytes);
        return true;
    }

    private static int FindSeparatorIndex(ReadOnlySpan<byte> row)
    {
        for (var index = 0; index < row.Length - 1; index++)
        {
            if (row[index] == (byte)'.' && row[index + 1] == (byte)' ')
            {
                return index;
            }
        }

        return -1;
    }
}
