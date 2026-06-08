using LargeFile.Sorter.Services.Models;

namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Reads sorted items sequentially from a temporary file.
/// </summary>
public interface ITempFileReader : IAsyncDisposable
{
    /// <summary>
    /// Reads the next available item from the temporary file or returns <c>null</c> when the file is exhausted.
    /// </summary>
    ValueTask<Item?> ReadNextAsync(CancellationToken cancellationToken = default);
}
