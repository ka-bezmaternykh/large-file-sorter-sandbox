using LargeFile.Sorter.Services.Models;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class ChunkFileReaderTests
{
    [Fact]
    public async Task ReadNextAsync_ShouldReadAllItemsFromChunkFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempFilePath = Path.Combine(tempDirectoryPath, "chunk.txt");
        await File.WriteAllTextAsync(tempFilePath, "1. Apple\n2. Banana\n3. Cherry");

        try
        {
            var adapter = new ChunkFileAdapter(
                new ChunkFileConfig
                {
                    FilePath = tempFilePath
                },
                NullLogger<ChunkFileAdapter>.Instance);
            await adapter.CompleteWriteAsync();

            await using var reader = new ChunkFileReader(
                adapter,
                new RowParser(),
                NullLogger<ChunkFileReader>.Instance);

            var first = await reader.ReadNextAsync();
            var second = await reader.ReadNextAsync();
            var third = await reader.ReadNextAsync();
            var fourth = await reader.ReadNextAsync();

            Assert.NotNull(first);
            Assert.Equal(1, first.Value.Number);
            Assert.Equal("Apple"u8.ToArray(), first.Value.TextBytes);
            Assert.NotNull(second);
            Assert.Equal(2, second.Value.Number);
            Assert.Equal("Banana"u8.ToArray(), second.Value.TextBytes);
            Assert.NotNull(third);
            Assert.Equal(3, third.Value.Number);
            Assert.Equal("Cherry"u8.ToArray(), third.Value.TextBytes);
            Assert.Null(fourth);
        }
        finally
        {
            DeleteDirectory(tempDirectoryPath);
        }
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
