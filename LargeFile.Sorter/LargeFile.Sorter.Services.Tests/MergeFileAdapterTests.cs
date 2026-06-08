using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class MergeFileAdapterTests
{
    [Fact]
    public void OpenWriteStream_ShouldCreateConfiguredFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempFilePath = Path.Combine(tempDirectoryPath, "merge.txt");

        try
        {
            ITempFileAdapter adapter = CreateAdapter(tempFilePath);

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
    public async Task DisposeAsync_ShouldDisposeOwnedWriteStream()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempFilePath = Path.Combine(tempDirectoryPath, "merge.txt");

        try
        {
            ITempFileAdapter adapter = CreateAdapter(tempFilePath);
            var stream = adapter.OpenWriteStream();

            await adapter.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => _ = stream.Length);
            Assert.False(File.Exists(tempFilePath));
        }
        finally
        {
            DeleteDirectory(tempDirectoryPath);
        }
    }

    private static ITempFileAdapter CreateAdapter(string filePath)
    {
        return new MergeFileAdapter(
            new MergeFileConfig
            {
                FilePath = filePath
            },
            NullLogger<MergeFileAdapter>.Instance);
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
