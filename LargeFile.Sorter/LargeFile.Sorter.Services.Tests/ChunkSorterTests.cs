using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.Tests;

public class ChunkSorterTests
{
    [Fact]
    public async Task ChunkSorter_ShouldSortItemsBeforeWritingChunkFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var services = CreateServices(tempDirectoryPath, 4096);

        await using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IChunkSorterFactory>();
        var sorter = await factory.CreateAsync();

        await sorter.Writer.WriteAsync("3. Banana\n1. Apple\n2. Apple\n"u8.ToArray());
        await sorter.Writer.CompleteAsync();
        await factory.WaitAllAsync();

        Assert.Equal("1. Apple\n2. Apple\n3. Banana\n", await File.ReadAllTextAsync(factory.ChunkFileAdapters[1].FilePath));

        DeleteDirectory(tempDirectoryPath);
    }

    private static ServiceCollection CreateServices(string tempDirectoryPath, int chunkSize)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SorterRunOptions
        {
            FilePath = Path.Combine(tempDirectoryPath, "input.txt"),
            OutputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt"),
            Force = true
        });
        services.AddSingleton(new ChunkConfig
        {
            ChunkSize = chunkSize,
            TempFilesFolder = tempDirectoryPath,
            TempFileTemplate = "chunk-{0:D4}.tmp"
        });
        services.AddLargeFileSorterServices();
        return services;
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
