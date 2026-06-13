using LargeFile.Sorter.Config;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter;

public static class AppHost
{
    private const string DefaultTempFilesFolder = "./temp";

    public static IHost Build(string[] args, CommandLineOptions options)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(options);

        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });

        builder.Services.AddSingleton(new SorterRunOptions
        {
            FilePath = options.File!,
            OutputFilePath = options.OutputFile!,
            Force = options.Force
        });
        builder.Services.AddSingleton(new ChunkConfig
        {
            // 128 MB
            ChunkSize = 128 * 1024 * 1024,
            TempFilesFolder = options.TempFilesDir ?? DefaultTempFilesFolder,
            TempFileTemplate = "chunk-{0:D4}.tmp"
        });
        builder.Services.AddSingleton(new MergeConfig
        {
            MaxChunkFilesPerMerge = 64,
            MaxConcurrentMergeBatches = 4,
            TempFilesFolder = options.TempFilesDir ?? DefaultTempFilesFolder,
            MergeFileTemplate = "merge-{0:D4}.tmp"
        });
        builder.Services.AddSingleton(new ChunkingProgressConfig
        {
            ReportInterval = TimeSpan.FromSeconds(5)
        });
        builder.Services.AddLargeFileSorterServices();

        return builder.Build();
    }
}
