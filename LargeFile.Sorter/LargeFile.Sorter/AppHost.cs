using LargeFile.Sorter.Config;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter;

public static class AppHost
{
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
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        builder.Services.AddSingleton(new SorterRunOptions
        {
            FilePath = options.File!,
            OutputFilePath = options.OutputFile!,
            Force = options.Force
        });
        builder.Services.AddLargeFileSorterServices();

        return builder.Build();
    }
}
