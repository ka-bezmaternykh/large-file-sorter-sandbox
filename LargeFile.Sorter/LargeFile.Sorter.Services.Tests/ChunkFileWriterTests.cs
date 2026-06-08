using System.Text;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class ChunkFileWriterTests
{
    [Fact]
    public async Task WriteAsync_ShouldBufferAndPersistDataOnDispose()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempFilePath = Path.Combine(tempDirectoryPath, "chunk.txt");

        try
        {
            await using (var writer = CreateWriter(tempFilePath))
            {
                await writer.WriteAsync(Encoding.UTF8.GetBytes("1. Apple\n"));
                await writer.WriteAsync(Encoding.UTF8.GetBytes("2. Banana\n"));
            }

            Assert.Equal("1. Apple\n2. Banana\n", File.ReadAllText(tempFilePath));
        }
        finally
        {
            DeleteDirectory(tempDirectoryPath);
        }
    }

    [Fact]
    public async Task FlushAsync_ShouldPersistBufferedDataImmediately()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempFilePath = Path.Combine(tempDirectoryPath, "chunk.txt");

        try
        {
            await using var writer = CreateWriter(tempFilePath);
            await writer.WriteAsync(Encoding.UTF8.GetBytes("1. Apple\n"));
            await writer.FlushAsync();
        }
        finally
        {
            Assert.Equal("1. Apple\n", File.ReadAllText(tempFilePath));
            DeleteDirectory(tempDirectoryPath);
        }
    }

    private static ITempFileWriter CreateWriter(string filePath)
    {
        var adapter = new ChunkFileAdapter(
            new ChunkFileConfig
            {
                FilePath = filePath
            },
            NullLogger<ChunkFileAdapter>.Instance);

        return new ChunkFileWriter(adapter, NullLogger<ChunkFileWriter>.Instance);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
