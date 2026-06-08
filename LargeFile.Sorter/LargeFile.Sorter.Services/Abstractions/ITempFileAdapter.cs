namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Owns the target path of a temporary file and manages its read/write stream lifecycle.
/// </summary>
public interface ITempFileAdapter : IAsyncDisposable
{
    /// <summary>
    /// Gets the full path of the temporary file managed by this adapter.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Opens a readable stream for the temporary file.
    /// </summary>
    FileStream OpenReadStream();

    /// <summary>
    /// Opens a writable stream for the temporary file.
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
