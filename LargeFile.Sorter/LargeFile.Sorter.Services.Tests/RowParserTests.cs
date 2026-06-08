using System.Buffers;

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

    [Fact]
    public void TryParseSequence_ShouldReturnParsedItemForValidRowAcrossSegments()
    {
        var parser = new RowParser();
        var row = CreateSequence("415. Ap"u8.ToArray(), "ple\r"u8.ToArray());

        var parsed = parser.TryParse(row, out var item);

        Assert.True(parsed);
        Assert.Equal(415, item.Number);
        Assert.Equal("Apple"u8.ToArray(), item.TextBytes);
    }

    private static ReadOnlySequence<byte> CreateSequence(byte[] first, byte[] second)
    {
        var firstSegment = new BufferSegment(first);
        var secondSegment = firstSegment.Append(second);
        return new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondSegment.Memory.Length);
    }

    private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public BufferSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new BufferSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;
            return segment;
        }
    }
}
