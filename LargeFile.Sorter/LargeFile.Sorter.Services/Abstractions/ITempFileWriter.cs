namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Buffers and persists bytes to a temporary file stream.
/// </summary>
public interface ITempFileWriter : IAsyncDisposable
{
    /// <summary>
    /// Writes the provided bytes into the buffered temporary file output pipeline.
    /// </summary>
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces any buffered bytes to be written to the underlying temporary file.
    /// </summary>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
