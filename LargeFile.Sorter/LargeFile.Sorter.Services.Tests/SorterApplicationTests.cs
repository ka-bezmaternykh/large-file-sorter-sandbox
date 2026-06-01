using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.Tests;

public class SorterApplicationTests
{
    [Fact]
    public async Task RunAsync_ShouldSplitInputIntoChunkFiles()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var inputFilePath = Path.Combine(tempDirectoryPath, "input.txt");
        await File.WriteAllTextAsync(inputFilePath, "1. Apple\n2. Banana\n3. Cherry\n");

        await using var serviceProvider = BuildServiceProvider(inputFilePath, tempDirectoryPath);
        var application = serviceProvider.GetRequiredService<ISorterApplication>();
        var factory = serviceProvider.GetRequiredService<IChunkSorterFactory>();

        await application.RunAsync();

        Assert.Equal(3, factory.ChunkFileAdapters.Count);
        Assert.Equal("1. Apple\n", await File.ReadAllTextAsync(factory.ChunkFileAdapters[1].FilePath));
        Assert.Equal("2. Banana\n", await File.ReadAllTextAsync(factory.ChunkFileAdapters[2].FilePath));
        Assert.Equal("3. Cherry\n", await File.ReadAllTextAsync(factory.ChunkFileAdapters[3].FilePath));

        DeleteDirectory(tempDirectoryPath);
    }

    [Fact]
    public async Task RunAsync_ShouldNotCreateChunkFilesWhenInputFileDoesNotExist()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var inputFilePath = Path.Combine(tempDirectoryPath, "missing.txt");

        await using var serviceProvider = BuildServiceProvider(inputFilePath, tempDirectoryPath);
        var application = serviceProvider.GetRequiredService<ISorterApplication>();
        var factory = serviceProvider.GetRequiredService<IChunkSorterFactory>();

        await application.RunAsync();

        Assert.Empty(factory.ChunkFileAdapters);

        DeleteDirectory(tempDirectoryPath);
    }

    [Fact]
    public async Task RunAsync_ShouldNotCreateChunkFilesWhenInputFileIsEmpty()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var inputFilePath = Path.Combine(tempDirectoryPath, "input.txt");
        await File.WriteAllBytesAsync(inputFilePath, []);

        await using var serviceProvider = BuildServiceProvider(inputFilePath, tempDirectoryPath);
        var application = serviceProvider.GetRequiredService<ISorterApplication>();
        var factory = serviceProvider.GetRequiredService<IChunkSorterFactory>();

        await application.RunAsync();

        Assert.Empty(factory.ChunkFileAdapters);

        DeleteDirectory(tempDirectoryPath);
    }

    private static ServiceProvider BuildServiceProvider(string inputFilePath, string tempDirectoryPath)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SorterRunOptions
        {
            FilePath = inputFilePath,
            OutputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt"),
            Force = true
        });
        services.AddSingleton(new ChunkConfig
        {
            ChunkSize = 1030,
            TempFilesFolder = tempDirectoryPath,
            TempFileTemplate = "chunk-{0:D4}.tmp"
        });
        services.AddLargeFileSorterServices();

        return services.BuildServiceProvider();
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
