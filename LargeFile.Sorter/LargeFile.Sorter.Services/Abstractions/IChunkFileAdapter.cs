namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Owns the target path of a single chunk file and creates write streams for it.
/// </summary>
public interface IChunkFileAdapter
{
    /// <summary>
    /// Gets the full path of the chunk file managed by this adapter.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Opens a writable stream for the chunk file.
    /// </summary>
    FileStream OpenWriteStream();
}
