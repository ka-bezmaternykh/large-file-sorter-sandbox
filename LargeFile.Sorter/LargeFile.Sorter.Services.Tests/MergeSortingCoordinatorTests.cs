using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.Tests;

public class MergeSortingCoordinatorTests
{
    [Fact]
    public async Task PromoteFinalAsync_ShouldPromoteSingleChunkFileToOutputFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var outputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt");
        var chunkPath = Path.Combine(tempDirectoryPath, "chunk-0001.tmp");
        await File.WriteAllTextAsync(chunkPath, "1. Apple\n");

        await using var serviceProvider = BuildServiceProvider(tempDirectoryPath, outputFilePath);
        var coordinator = serviceProvider.GetRequiredService<IMergeSortingCoordinator>();

        coordinator.Start(new Dictionary<int, ITempFileAdapter>
        {
            [1] = await CreateCompletedAdapterAsync(chunkPath)
        });

        await coordinator.PromoteFinalAsync();

        Assert.Equal("1. Apple\n", await File.ReadAllTextAsync(outputFilePath));
        Assert.False(File.Exists(chunkPath));

        DeleteDirectory(tempDirectoryPath);
    }

    [Fact]
    public async Task MergePipeline_ShouldMergeChunkFilesIntoOutputFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var outputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt");
        var firstChunkPath = Path.Combine(tempDirectoryPath, "chunk-0001.tmp");
        var secondChunkPath = Path.Combine(tempDirectoryPath, "chunk-0002.tmp");

        await File.WriteAllTextAsync(firstChunkPath, "2. Apple\n4. Cherry\n");
        await File.WriteAllTextAsync(secondChunkPath, "1. Apple\n3. Banana\n");

        await using var serviceProvider = BuildServiceProvider(tempDirectoryPath, outputFilePath);
        var coordinator = serviceProvider.GetRequiredService<IMergeSortingCoordinator>();

        coordinator.Start(new Dictionary<int, ITempFileAdapter>
        {
            [1] = await CreateCompletedAdapterAsync(firstChunkPath),
            [2] = await CreateCompletedAdapterAsync(secondChunkPath)
        });

        await RunMergeLoopAsync(coordinator);

        Assert.Equal("1. Apple\n2. Apple\n3. Banana\n4. Cherry\n", await File.ReadAllTextAsync(outputFilePath));
        Assert.False(File.Exists(firstChunkPath));
        Assert.False(File.Exists(secondChunkPath));

        DeleteDirectory(tempDirectoryPath);
    }

    [Fact]
    public async Task MergePipeline_ShouldMergeMultiplePassesWhenBatchLimitIsExceeded()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var outputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt");

        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0001.tmp"), "2. Apple\n");
        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0002.tmp"), "4. Banana\n");
        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0003.tmp"), "1. Apple\n");
        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0004.tmp"), "5. Cherry\n");
        await File.WriteAllTextAsync(Path.Combine(tempDirectoryPath, "chunk-0005.tmp"), "3. Banana\n");

        await using var serviceProvider = BuildServiceProvider(tempDirectoryPath, outputFilePath, maxChunkFilesPerMerge: 2);
        var coordinator = serviceProvider.GetRequiredService<IMergeSortingCoordinator>();

        coordinator.Start(new Dictionary<int, ITempFileAdapter>
        {
            [1] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0001.tmp")),
            [2] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0002.tmp")),
            [3] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0003.tmp")),
            [4] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0004.tmp")),
            [5] = await CreateCompletedAdapterAsync(Path.Combine(tempDirectoryPath, "chunk-0005.tmp"))
        });

        await RunMergeLoopAsync(coordinator);

        Assert.Equal("1. Apple\n2. Apple\n3. Banana\n4. Banana\n5. Cherry\n", await File.ReadAllTextAsync(outputFilePath));
        Assert.Empty(Directory.GetFiles(tempDirectoryPath, "*.tmp"));

        DeleteDirectory(tempDirectoryPath);
    }

    private static async Task RunMergeLoopAsync(IMergeSortingCoordinator coordinator)
    {
        while (coordinator.HasMoreThanOneFile)
        {
            while (coordinator.HasNextBatch)
            {
                await coordinator.MergeNextBatchAsync();
            }

            await coordinator.CompleteCurrentPassAsync();
        }

        await coordinator.PromoteFinalAsync();
    }

    private static ServiceProvider BuildServiceProvider(
        string tempDirectoryPath,
        string outputFilePath,
        int maxChunkFilesPerMerge = 64,
        int maxConcurrentMergeBatches = 4)
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
            MaxConcurrentMergeBatches = maxConcurrentMergeBatches,
            TempFilesFolder = tempDirectoryPath,
            MergeFileTemplate = "merge-{0:D4}.tmp"
        });
        services.AddSingleton(new ChunkConfig
        {
            ChunkSize = 128 * 1024 * 1024,
            TempFilesFolder = tempDirectoryPath,
            TempFileTemplate = "chunk-{0:D4}.tmp"
        });
        services.AddSingleton(new ChunkingProgressConfig
        {
            ReportInterval = TimeSpan.FromSeconds(5)
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
