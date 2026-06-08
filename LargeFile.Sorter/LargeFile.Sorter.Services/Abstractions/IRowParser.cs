using System.Buffers;
using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Parses a single input row into the internal sortable item representation.
/// </summary>
public interface IRowParser
{
    /// <summary>
    /// Attempts to parse one input row without the trailing newline.
    /// </summary>
    bool TryParse(ReadOnlySpan<byte> row, out Item item);

    /// <summary>
    /// Attempts to parse one input row from a segmented buffer without the trailing newline.
    /// </summary>
    bool TryParse(ReadOnlySequence<byte> row, out Item item);
}
