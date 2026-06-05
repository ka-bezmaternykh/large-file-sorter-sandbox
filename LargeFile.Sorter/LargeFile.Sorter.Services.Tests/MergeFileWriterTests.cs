using System.Text;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class MergeFileWriterTests
{
    [Fact]
    public async Task DisposeAsync_ShouldFlushBufferedBytesAndCompleteAdapterWrite()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempFilePath = Path.Combine(tempDirectoryPath, "merge.txt");

        try
        {
            var adapter = new MergeFileAdapter(
                new MergeFileConfig
                {
                    FilePath = tempFilePath
                },
                NullLogger<MergeFileAdapter>.Instance);

            await using (var writer = new MergeFileWriter(adapter, NullLogger<MergeFileWriter>.Instance))
            {
                await writer.WriteAsync(Encoding.UTF8.GetBytes("1. Apple\n"));
            }

            Assert.Equal("1. Apple\n", await File.ReadAllTextAsync(tempFilePath));
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
