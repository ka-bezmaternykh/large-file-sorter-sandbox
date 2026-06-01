using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class ChunkFileAdapterTests
{
    [Fact]
    public void OpenWriteStream_ShouldCreateConfiguredFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempFilePath = Path.Combine(tempDirectoryPath, "chunk.txt");

        try
        {
            IChunkFileAdapter adapter = CreateAdapter(tempFilePath);

            using var stream = adapter.OpenWriteStream();

            Assert.Equal(tempFilePath, adapter.FilePath);
            Assert.True(stream.CanWrite);
            Assert.True(File.Exists(tempFilePath));
        }
        finally
        {
            DeleteDirectory(tempDirectoryPath);
        }
    }

    [Fact]
    public void OpenWriteStream_ShouldReplaceExistingFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempFilePath = Path.Combine(tempDirectoryPath, "chunk.txt");
        File.WriteAllText(tempFilePath, "existing");

        try
        {
            IChunkFileAdapter adapter = CreateAdapter(tempFilePath);

            using var stream = adapter.OpenWriteStream();
            using var writer = new StreamWriter(stream);
            writer.Write("new");
            writer.Flush();
        }
        finally
        {
            Assert.Equal("new", File.ReadAllText(tempFilePath));
            DeleteDirectory(tempDirectoryPath);
        }
    }

    private static IChunkFileAdapter CreateAdapter(string filePath)
    {
        return new ChunkFileAdapter(
            new ChunkFileConfig
            {
                FilePath = filePath
            },
            NullLogger<ChunkFileAdapter>.Instance);
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
