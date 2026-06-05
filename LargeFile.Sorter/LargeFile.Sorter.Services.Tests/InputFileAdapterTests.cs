using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class InputFileAdapterTests
{
    [Fact]
    public void TryOpenReadStream_ShouldOpenConfiguredFile()
    {
        var tempFilePath = CreateTempFile("415. Apple\n");

        try
        {
            IInputFileAdapter adapter = new InputFileAdapter(new InputFileConfig
            {
                FilePath = tempFilePath
            }, NullLogger<InputFileAdapter>.Instance);

            var success = adapter.TryOpenReadStream(out var stream);

            Assert.True(success);
            Assert.NotNull(stream);
            using (stream)
            {
                Assert.True(stream.CanRead);
            }
            Assert.Equal(tempFilePath, adapter.FilePath);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public void TryOpenReadStream_ShouldReturnFalseWhenFileDoesNotExist()
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        IInputFileAdapter adapter = new InputFileAdapter(new InputFileConfig
        {
            FilePath = tempFilePath
        }, NullLogger<InputFileAdapter>.Instance);

        var success = adapter.TryOpenReadStream(out var stream);

        Assert.False(success);
        Assert.Null(stream);
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeOwnedReadStream()
    {
        var tempFilePath = CreateTempFile("415. Apple\n");

        try
        {
            IInputFileAdapter adapter = new InputFileAdapter(new InputFileConfig
            {
                FilePath = tempFilePath
            }, NullLogger<InputFileAdapter>.Instance);

            var success = adapter.TryOpenReadStream(out var stream);

            Assert.True(success);
            Assert.NotNull(stream);

            await adapter.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => _ = stream!.Length);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, content);
        return path;
    }
}
