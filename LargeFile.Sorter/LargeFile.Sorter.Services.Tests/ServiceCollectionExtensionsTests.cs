using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddLargeFileSorterServices_RegistersSorterApplication()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var tempInputFilePath = Path.Combine(tempDirectoryPath, "input.txt");
        await File.WriteAllTextAsync(tempInputFilePath, "1. Apple\n");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SorterRunOptions
        {
            FilePath = tempInputFilePath,
            OutputFilePath = Path.Combine(tempDirectoryPath, "output.txt"),
            Force = true
        });
        services.AddSingleton(new ChunkConfig
        {
            ChunkSize = 1030,
            TempFilesFolder = tempDirectoryPath,
            TempFileTemplate = "chunk-{0:D4}.tmp"
        });
        services.AddSingleton(new MergeConfig
        {
            MaxChunkFilesPerMerge = 64,
            MaxConcurrentMergeBatches = 4,
            TempFilesFolder = tempDirectoryPath,
            MergeFileTemplate = "merge-{0:D4}.tmp"
        });
        services.AddSingleton(new ChunkingProgressConfig
        {
            ReportInterval = TimeSpan.FromSeconds(5)
        });
        services.AddSingleton(new MergeProgressConfig
        {
            ReportInterval = TimeSpan.FromSeconds(5),
            BatchReportInterval = TimeSpan.FromSeconds(5)
        });
        services.AddLargeFileSorterServices();

        await using var serviceProvider = services.BuildServiceProvider();

        var application = serviceProvider.GetRequiredService<ISorterApplication>();
        var adapter = serviceProvider.GetRequiredService<IInputFileAdapter>();
        var chunkingProgressReporter = serviceProvider.GetRequiredService<IChunkingProgressReporter>();
        var mergeProgressReporter = serviceProvider.GetRequiredService<IMergeProgressReporter>();
        var chunkingProgressConfig = serviceProvider.GetRequiredService<ChunkingProgressConfig>();
        var mergeProgressConfig = serviceProvider.GetRequiredService<MergeProgressConfig>();
        var environmentMonitor = serviceProvider.GetRequiredService<IEnvironmentMonitor>();
        var chunkSorterFactory = serviceProvider.GetRequiredService<IChunkSorterFactory>();
        var mergeSortingCoordinator = serviceProvider.GetRequiredService<IMergeSortingCoordinator>();
        var mergeExecutionLimiter = serviceProvider.GetRequiredService<IMergeExecutionLimiter>();
        var mergeBatchProgressReporterFactory = serviceProvider.GetRequiredService<IMergeBatchProgressReporterFactory>();
        var itemFormatter = serviceProvider.GetRequiredService<IItemFormatter>();

        Assert.Equal(tempInputFilePath, adapter.FilePath);
        Assert.NotNull(chunkingProgressReporter);
        Assert.NotNull(mergeProgressReporter);
        Assert.Equal(TimeSpan.FromSeconds(5), chunkingProgressConfig.ReportInterval);
        Assert.Equal(TimeSpan.FromSeconds(5), mergeProgressConfig.ReportInterval);
        Assert.Equal(TimeSpan.FromSeconds(5), mergeProgressConfig.BatchReportInterval);
        Assert.True(environmentMonitor.LevelOfParallelism > 0);
        Assert.Empty(chunkSorterFactory.ChunkFileAdapters);
        Assert.NotNull(mergeSortingCoordinator);
        Assert.True(mergeExecutionLimiter.MaxConcurrentBatches > 0);
        Assert.NotNull(mergeBatchProgressReporterFactory);
        Assert.IsType<TextRowFormatter>(itemFormatter);

        await application.RunAsync();

        Assert.Single(chunkSorterFactory.ChunkFileAdapters);
        Assert.Equal("1. Apple\n", await File.ReadAllTextAsync(Path.Combine(tempDirectoryPath, "output.txt")));
        Assert.False(File.Exists(chunkSorterFactory.ChunkFileAdapters[1].FilePath));

        DeleteDirectory(tempDirectoryPath);
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
