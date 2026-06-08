using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.Tests;

public class MergeSorterTests
{
    [Fact]
    public async Task MergeAsync_ShouldMergeChunkFilesIntoOutputFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var outputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt");
        var firstChunkPath = Path.Combine(tempDirectoryPath, "chunk-0001.tmp");
        var secondChunkPath = Path.Combine(tempDirectoryPath, "chunk-0002.tmp");

        await File.WriteAllTextAsync(firstChunkPath, "2. Apple\n4. Cherry\n");
        await File.WriteAllTextAsync(secondChunkPath, "1. Apple\n3. Banana\n");

        await using var serviceProvider = BuildServiceProvider(tempDirectoryPath, outputFilePath);
        var mergeSorter = serviceProvider.GetRequiredService<IMergeSorter>();

        var chunkFileAdapters = new Dictionary<int, ITempFileAdapter>
        {
            [1] = await CreateCompletedAdapterAsync(firstChunkPath),
            [2] = await CreateCompletedAdapterAsync(secondChunkPath)
        };

        await mergeSorter.MergeAsync(chunkFileAdapters);

        Assert.Equal("1. Apple\n2. Apple\n3. Banana\n4. Cherry\n", await File.ReadAllTextAsync(outputFilePath));
        Assert.False(File.Exists(firstChunkPath));
        Assert.False(File.Exists(secondChunkPath));

        DeleteDirectory(tempDirectoryPath);
    }

    [Fact]
    public async Task MergeAsync_ShouldPromoteSingleChunkFileToOutputFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var outputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt");
        var chunkPath = Path.Combine(tempDirectoryPath, "chunk-0001.tmp");
        await File.WriteAllTextAsync(chunkPath, "1. Apple\n");

        await using var serviceProvider = BuildServiceProvider(tempDirectoryPath, outputFilePath);
        var mergeSorter = serviceProvider.GetRequiredService<IMergeSorter>();

        var chunkFileAdapters = new Dictionary<int, ITempFileAdapter>
        {
            [1] = await CreateCompletedAdapterAsync(chunkPath)
        };

        await mergeSorter.MergeAsync(chunkFileAdapters);

        Assert.Equal("1. Apple\n", await File.ReadAllTextAsync(outputFilePath));
        Assert.False(File.Exists(chunkPath));

        DeleteDirectory(tempDirectoryPath);
    }

    [Fact]
    public async Task MergeAsync_ShouldMergeMultiplePassesWhenBatchLimitIsExceeded()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var outputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt");

        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0001.tmp"), "2. Apple\n");
        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0002.tmp"), "4. Banana\n");
        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0003.tmp"), "1. Apple\n");
        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0004.tmp"), "5. Cherry\n");
        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0005.tmp"), "3. Banana\n");

        await using var serviceProvider = BuildServiceProvider(tempDirectoryPath, outputFilePath, maxChunkFilesPerMerge: 2);
        var mergeSorter = serviceProvider.GetRequiredService<IMergeSorter>();

        var chunkFileAdapters = new Dictionary<int, ITempFileAdapter>
        {
            [1] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0001.tmp")),
            [2] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0002.tmp")),
            [3] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0003.tmp")),
            [4] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0004.tmp")),
            [5] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0005.tmp"))
        };

        await mergeSorter.MergeAsync(chunkFileAdapters);

        Assert.Equal("1. Apple\n2. Apple\n3. Banana\n4. Banana\n5. Cherry\n", await File.ReadAllTextAsync(outputFilePath));
        Assert.Empty(Directory.GetFiles(tempDirectoryPath, "*.tmp"));

        DeleteDirectory(tempDirectoryPath);
    }

    private static ServiceProvider BuildServiceProvider(string tempDirectoryPath, string outputFilePath, int maxChunkFilesPerMerge = 64)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SorterRunOptions
        {
            FilePath = Path.Combine(tempDirectoryPath, "input.txt"),
            OutputFilePath = outputFilePath,
            Force = true
        });
        services.AddSingleton(new MergeConfig
        {
            MaxChunkFilesPerMerge = maxChunkFilesPerMerge,
            TempFilesFolder = tempDirectoryPath,
            MergeFileTemplate = "merge-{0:D4}.tmp"
        });
        services.AddLargeFileSorterServices();
        return services.BuildServiceProvider();
    }

    private static async Task<ITempFileAdapter> CreateCompletedAdapterAsync(string filePath)
    {
        var adapter = new ChunkFileAdapter(
            new ChunkFileConfig
            {
                FilePath = filePath
            },
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ChunkFileAdapter>.Instance);
        await adapter.CompleteWriteAsync();
        return adapter;
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
