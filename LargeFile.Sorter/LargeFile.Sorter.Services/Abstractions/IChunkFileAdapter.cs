namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Owns the target path of a single chunk file and creates read/write streams for it.
/// </summary>
public interface IChunkFileAdapter : IAsyncDisposable
{
    /// <summary>
    /// Gets the full path of the chunk file managed by this adapter.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Opens a readable stream for the chunk file.
    /// </summary>
    FileStream OpenReadStream();

    /// <summary>
    /// Opens a writable stream for the chunk file.
    /// </summary>
    FileStream OpenWriteStream();

    /// <summary>
    /// Completes the write lifecycle and releases the write stream owned by the adapter.
    /// </summary>
    ValueTask CompleteWriteAsync();

    /// <summary>
    /// Completes the read lifecycle and releases the read stream owned by the adapter.
    /// </summary>
    ValueTask CompleteReadAsync();
}
