using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Converts sorted items into the target chunk row representation.
/// </summary>
public interface IItemFormatter
{
    /// <summary>
    /// Attempts to write a single formatted item into the provided destination buffer.
    /// </summary>
    bool TryFormat(Item item, Span<byte> destination, out int written);
}
