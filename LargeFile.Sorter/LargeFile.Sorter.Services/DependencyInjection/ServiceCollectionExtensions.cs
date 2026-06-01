using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLargeFileSorterServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<SorterRunOptions>();
            return new InputFileConfig
            {
                FilePath = options.FilePath
            };
        });
        services.AddSingleton<IEnvironmentMonitor, EnvironmentMonitor>();
        services.AddSingleton<IChunkExecutionLimiter, ChunkExecutionLimiter>();
        services.AddSingleton<IInputFileAdapter, InputFileAdapter>();
        services.AddTransient<IRowParser, RowParser>();
        services.AddTransient<IComparer<Models.Item>, ItemComparer>();
        services.AddTransient<IItemFormatter, TextRowFormatter>();
        // TODO Binary file optimization
        //services.AddSingleton<BinaryRowFormatter>();
        services.AddTransient<IInputFileReader, InputFileReader>();
        services.AddSingleton<IChunkSorterFactory, ChunkSorterFactory>();
        services.AddSingleton<ISorterApplication, SorterApplication>();

        return services;
    }
}
