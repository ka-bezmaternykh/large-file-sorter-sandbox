using System.IO.Pipelines;
using System.Text;
using System.Buffers;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class InputFileReaderTests
{
    [Fact]
    public async Task HasNextAsync_ShouldReturnFalseWhenInputFileDoesNotExist()
    {
        var missingFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        await using var reader = CreateReader(missingFilePath);

        var hasNext = await reader.HasNextAsync();

        Assert.False(hasNext);
    }

    [Fact]
    public async Task ReadNextAsync_ShouldWriteSingleLineWhenChunkThresholdFallsInsideIt()
    {
        var tempFilePath = CreateTempFile("1. Apple\n2. Banana\n");

        try
        {
            await using var reader = CreateReader(tempFilePath);

            Assert.True(await reader.HasNextAsync());

            var pipe = new Pipe();
            await reader.ReadNextAsync(pipe.Writer, 1030);
            var content = await ReadPipeContentAsync(pipe);

            Assert.Equal("1. Apple\n", content);
            Assert.True(await reader.HasNextAsync());
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ReadNextAsync_ShouldPreserveRemainingDataAcrossCalls()
    {
        var tempFilePath = CreateTempFile("1. Apple\n2. Banana\n3. Cherry\n");

        try
        {
            await using var reader = CreateReader(tempFilePath);

            var first = await ReadChunkAsync(reader, 1030);
            var second = await ReadChunkAsync(reader, 1030);
            var third = await ReadChunkAsync(reader, 1030);

            Assert.Equal("1. Apple\n", first);
            Assert.Equal("2. Banana\n", second);
            Assert.Equal("3. Cherry\n", third);
            Assert.False(await reader.HasNextAsync());
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ReadNextAsync_ShouldReturnRemainingBytesAtEndOfFileWithoutTrailingNewLine()
    {
        var tempFilePath = CreateTempFile("1. Apple\n2. Banana");

        try
        {
            await using var reader = CreateReader(tempFilePath);

            var first = await ReadChunkAsync(reader, 1030);
            var second = await ReadChunkAsync(reader, 1030);

            Assert.Equal("1. Apple\n", first);
            Assert.Equal("2. Banana", second);
            Assert.False(await reader.HasNextAsync());
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ReadNextAsync_ShouldThrowForNonPositiveChunkSize()
    {
        var tempFilePath = CreateTempFile("1. Apple\n");

        try
        {
            await using var reader = CreateReader(tempFilePath);
            var pipe = new Pipe();

            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await reader.ReadNextAsync(pipe.Writer, 0));

            Assert.Equal("chunkSize", exception.ParamName);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    private static async Task<string> ReadChunkAsync(
        IInputFileReader reader,
        int chunkSize)
    {
        var pipe = new Pipe();
        await reader.ReadNextAsync(pipe.Writer, chunkSize);
        return await ReadPipeContentAsync(pipe);
    }

    private static async Task<string> ReadPipeContentAsync(Pipe pipe)
    {
        var readResult = await pipe.Reader.ReadAsync();
        ReadOnlySequence<byte> buffer = readResult.Buffer;
        var bytes = buffer.ToArray();
        pipe.Reader.AdvanceTo(buffer.End);
        await pipe.Reader.CompleteAsync();
        return Encoding.UTF8.GetString(bytes);
    }

    private static IInputFileReader CreateReader(string filePath)
    {
        var adapter = new InputFileAdapter(
            new InputFileConfig
            {
                FilePath = filePath
            },
            NullLogger<InputFileAdapter>.Instance);

        return new InputFileReader(
            adapter,
            new ChunkingProgressReporter(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ChunkingProgressReporter>.Instance,
                new ChunkingProgressConfig
                {
                    ReportInterval = TimeSpan.FromSeconds(5)
                }),
            NullLogger<InputFileReader>.Instance);
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, content);
        return path;
    }
}
