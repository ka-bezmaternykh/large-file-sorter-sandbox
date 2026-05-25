using DummyFile.Generator.Services.Abstract;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace DummyFile.Generator.Services.Tests;

public class FileExporterTests
{
    [Fact]
    public async Task TryExportAsync_ShouldWriteRowsInExpectedFormat()
    {
        var fileAdapter = new InMemoryFileAdapter();

        await using (IFileExporter exporter = new FileExporter(
                         fileAdapter,
                         NullLogger<FileExporter>.Instance,
                         new FileExporterConfig()))
        {
            Assert.True(await exporter.TryExportAsync("415. Apple\n"u8.ToArray()));
            Assert.True(await exporter.TryExportAsync("2. Banana is yellow\n"u8.ToArray()));
        }

        var content = Encoding.UTF8.GetString(fileAdapter.WrittenBytes.ToArray());

        Assert.Equal("415. Apple\n2. Banana is yellow\n", content);
    }

    [Fact]
    public async Task TryExportAsync_ShouldReturnFalseWhenMaxFileSizeWouldBeExceeded()
    {
        var fileAdapter = new InMemoryFileAdapter();

        await using IFileExporter exporter = new FileExporter(
            fileAdapter,
            NullLogger<FileExporter>.Instance,
            new FileExporterConfig
            {
                MaxFileSize = 10
            });

        Assert.True(await exporter.TryExportAsync("12345\n"u8.ToArray()));
        Assert.False(await exporter.TryExportAsync("67890\n"u8.ToArray()));
    }

    private sealed class InMemoryFileAdapter : IFileAdapter
    {
        private readonly MemoryStream _stream = new();

        public ReadOnlyMemory<byte> WrittenBytes => _stream.ToArray();

        public Stream CreateWriteStream()
        {
            return _stream;
        }
    }
}
