using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.Tests;

public class SorterApplicationTests
{
    [Fact]
    public async Task RunAsync_ShouldMergeChunkFilesIntoOutputFile()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var inputFilePath = Path.Combine(tempDirectoryPath, "input.txt");
        var outputFilePath = Path.Combine(tempDirectoryPath, "sorted.txt");
        await File.WriteAllTextAsync(inputFilePath, "2. Banana\n3. Cherry\n1. Apple\n");

        await using var serviceProvider = BuildServiceProvider(inputFilePath, tempDirectoryPath);
        var application = serviceProvider.GetRequiredService<ISorterApplication>();
        var factory = serviceProvider.GetRequiredService<IChunkSorterFactory>();

        await application.RunAsync();

        Assert.Equal(3, factory.ChunkFileAdapters.Count);
        Assert.Equal("1. Apple\n2. Banana\n3. Cherry\n", await File.ReadAllTextAsync(outputFilePath));
        Assert.All(factory.ChunkFileAdapters.Values, adapter => Assert.False(File.Exists(adapter.FilePath)));

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
        Assert.False(File.Exists(Path.Combine(tempDirectoryPath, "sorted.txt")));

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
        Assert.False(File.Exists(Path.Combine(tempDirectoryPath, "sorted.txt")));

        DeleteDirectory(tempDirectoryPath);
    }

    [Fact]
    public async Task RunAsync_ShouldMergeWhenMoreThanSixtyFourChunkFilesAreProduced()
    {
        var tempDirectoryPath = CreateTempDirectory();
        var inputFilePath = Path.Combine(tempDirectoryPath, "input.txt");
        var lines = Enumerable.Range(1, 65).Select(number => $"{number}. Item {number}");
        await File.WriteAllTextAsync(inputFilePath, string.Join('\n', lines) + '\n');

        await using var serviceProvider = BuildServiceProvider(inputFilePath, tempDirectoryPath);
        var application = serviceProvider.GetRequiredService<ISorterApplication>();
        var factory = serviceProvider.GetRequiredService<IChunkSorterFactory>();
        var expectedOutput = string.Join(
                                 '\n',
                                 Enumerable.Range(1, 65)
                                     .Select(number => (Number: number, Text: $"Item {number}"))
                                     .OrderBy(item => item.Text, StringComparer.Ordinal)
                                     .ThenBy(item => item.Number)
                                     .Select(item => $"{item.Number}. {item.Text}")) + '\n';

        await application.RunAsync();

        Assert.Equal(65, factory.ChunkFileAdapters.Count);
        Assert.Equal(expectedOutput, await File.ReadAllTextAsync(Path.Combine(tempDirectoryPath, "sorted.txt")));
        Assert.All(factory.ChunkFileAdapters.Values, adapter => Assert.False(File.Exists(adapter.FilePath)));

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
        services.AddSingleton(new MergeConfig
        {
            MaxChunkFilesPerMerge = 64,
            TempFilesFolder = tempDirectoryPath,
            MergeFileTemplate = "merge-{0:D4}.tmp"
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
