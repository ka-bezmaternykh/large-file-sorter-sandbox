namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Buffers and persists chunk bytes to the chunk file stream.
/// </summary>
public interface IChunkFileWriter : IAsyncDisposable
{
    /// <summary>
    /// Writes the provided bytes into the buffered chunk output pipeline.
    /// </summary>
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces any buffered bytes to be written to the underlying chunk file.
    /// </summary>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
