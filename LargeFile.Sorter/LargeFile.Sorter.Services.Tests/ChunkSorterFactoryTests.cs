using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.Tests;

public class ChunkSorterFactoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldGenerateUniqueChunkFileNamesAndTrackAdapters()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SorterRunOptions
        {
            FilePath = "input.txt",
            OutputFilePath = "output.txt",
            Force = true
        });
        services.AddSingleton(new ChunkConfig
        {
            ChunkSize = 1030,
            TempFilesFolder = tempDirectoryPath,
            TempFileTemplate = "chunk-{0:D4}.tmp"
        });
        services.AddSingleton(new ChunkingProgressConfig
        {
            ReportInterval = TimeSpan.FromSeconds(5)
        });
        services.AddSingleton(new MergeConfig
        {
            MaxChunkFilesPerMerge = 64,
            MaxConcurrentMergeBatches = 4,
            TempFilesFolder = tempDirectoryPath,
            MergeFileTemplate = "merge-{0:D4}.tmp"
        });
        services.AddLargeFileSorterServices();

        await using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IChunkSorterFactory>();

        var first = await factory.CreateAsync();
        var second = await factory.CreateAsync();

        await first.Writer.CompleteAsync();
        await second.Writer.CompleteAsync();
        await factory.WaitAllAsync();

        Assert.Equal(2, factory.ChunkFileAdapters.Count);
        Assert.Contains(1, factory.ChunkFileAdapters.Keys);
        Assert.Contains(2, factory.ChunkFileAdapters.Keys);
        Assert.All(factory.ChunkFileAdapters.Values, adapter => Assert.StartsWith(tempDirectoryPath, adapter.FilePath, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1030, first.ChunkSize);
        Assert.Equal(1030, second.ChunkSize);
        Assert.EndsWith("chunk-0001.tmp", factory.ChunkFileAdapters[1].FilePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("chunk-0002.tmp", factory.ChunkFileAdapters[2].FilePath, StringComparison.OrdinalIgnoreCase);

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
