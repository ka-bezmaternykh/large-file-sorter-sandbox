namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Buffers and persists merge output bytes to the merge file stream.
/// </summary>
public interface IMergeFileWriter : IAsyncDisposable
{
    /// <summary>
    /// Writes the provided bytes into the buffered merge output pipeline.
    /// </summary>
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces any buffered bytes to be written to the underlying merge file.
    /// </summary>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
