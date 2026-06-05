namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Owns the target path of a single merge file and creates write streams for it.
/// </summary>
public interface IMergeFileAdapter : IAsyncDisposable
{
    /// <summary>
    /// Gets the full path of the merge file managed by this adapter.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Opens a writable stream for the merge file.
    /// </summary>
    FileStream OpenWriteStream();

    /// <summary>
    /// Completes the write lifecycle and disposes the owned merge write stream.
    /// </summary>
    ValueTask CompleteWriteAsync();
}
