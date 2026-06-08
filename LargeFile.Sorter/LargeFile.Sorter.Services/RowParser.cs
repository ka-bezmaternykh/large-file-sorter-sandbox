using System.Buffers;
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

        if (row[^1] == (byte)'\r')
        {
            row = row[..^1];
        }

        var separatorIndex = FindSeparatorIndex(row);
        if (separatorIndex <= 0)
        {
            return false;
        }

        var numberBytes = row[..separatorIndex];
        if (!TryParseNumber(numberBytes, out var number))
        {
            return false;
        }

        // deliberate allocation, since item will outlive the buffer
        var textBytes = row[(separatorIndex + 2)..].ToArray();
        item = new Item(number, textBytes);
        return true;
    }

    public bool TryParse(ReadOnlySequence<byte> row, out Item item)
    {
        item = default;

        if (row.IsEmpty)
        {
            return false;
        }

        if (EndsWithCarriageReturn(row))
        {
            row = row.Slice(0, row.Length - 1);
        }

        var reader = new SequenceReader<byte>(row);
        if (!reader.TryReadTo(out ReadOnlySequence<byte> numberBytes, (byte)'.', advancePastDelimiter: false))
        {
            return false;
        }

        if (!reader.TryRead(out var dot) || dot != (byte)'.')
        {
            return false;
        }

        if (!reader.TryRead(out var space) || space != (byte)' ')
        {
            return false;
        }

        if (!TryParseNumber(numberBytes, out var number))
        {
            return false;
        }

        var textBytesLength = checked((int)reader.Remaining);
        var textBytes = new byte[textBytesLength];
        row.Slice(reader.Position).CopyTo(textBytes);
        item = new Item(number, textBytes);
        return true;
    }

    private static bool TryParseNumber(ReadOnlySpan<byte> numberBytes, out long number)
    {
        return Utf8Parser.TryParse(numberBytes, out number, out var consumed) &&
               consumed == numberBytes.Length &&
               number > 0;
    }

    private static bool TryParseNumber(ReadOnlySequence<byte> numberBytes, out long number)
    {
        if (numberBytes.IsSingleSegment)
        {
            return TryParseNumber(numberBytes.FirstSpan, out number);
        }

        var length = checked((int)numberBytes.Length);
        var rented = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            numberBytes.CopyTo(rented);
            return TryParseNumber(rented.AsSpan(0, length), out number);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
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

    private static bool EndsWithCarriageReturn(ReadOnlySequence<byte> row)
    {
        var lastIndex = row.Length - 1;
        var lastByteSequence = row.Slice(lastIndex, 1);
        return lastByteSequence.FirstSpan[0] == (byte)'\r';
    }
}
