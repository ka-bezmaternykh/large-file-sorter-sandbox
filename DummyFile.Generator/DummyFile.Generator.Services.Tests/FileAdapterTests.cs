using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace DummyFile.Generator.Services.Tests;

// These tests intentionally touch the real file system and are slightly integration-style
// to keep the file adapter implementation and test setup straightforward.
public class FileAdapterTests
{
    [Fact]
    public void CreateWriteStream_ShouldCreateDirectoryWhenItDoesNotExist()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(directoryPath, "items.txt");

        try
        {
            var fileAdapter = new FileAdapter(
                new FileAdapterConfig
                {
                    FilePath = filePath,
                    IsOverwriteAllowed = true
                },
                NullLogger<FileAdapter>.Instance);

            using var stream = fileAdapter.CreateWriteStream();
            stream.Write("1. Apple\n"u8);

            Assert.True(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateWriteStream_ShouldThrowWhenFileExistsAndForceIsFalse()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllText(filePath, "old content");
            var fileAdapter = new FileAdapter(
                new FileAdapterConfig
                {
                    FilePath = filePath,
                    IsOverwriteAllowed = false
                },
                NullLogger<FileAdapter>.Instance);

            var exception = Assert.Throws<IOException>(() => fileAdapter.CreateWriteStream());

            Assert.Contains("--force", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void CreateWriteStream_ShouldDeleteExistingFileWhenForceIsTrue()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllText(filePath, "old content");
            var fileAdapter = new FileAdapter(
                new FileAdapterConfig
                {
                    FilePath = filePath,
                    IsOverwriteAllowed = true
                },
                NullLogger<FileAdapter>.Instance);

            using (var stream = fileAdapter.CreateWriteStream())
            {
                stream.Write("1. Apple\n"u8);
            }

            var content = File.ReadAllText(filePath, Encoding.UTF8);
            Assert.Equal("1. Apple\n", content);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
