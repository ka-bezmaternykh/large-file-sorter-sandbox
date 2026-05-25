namespace DummyFile.Generator.Services.Abstract;

public interface IFileExporter : IDisposable, IAsyncDisposable
{
    ValueTask<bool> TryExportAsync(ReadOnlyMemory<byte> row, CancellationToken cancellationToken = default);
}
