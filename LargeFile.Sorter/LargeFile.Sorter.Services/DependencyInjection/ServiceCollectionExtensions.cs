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
        services.AddSingleton<IChunkingProgressReporter, ChunkingProgressReporter>();
        services.AddSingleton<IMergeProgressReporter, MergeProgressReporter>();
        services.AddSingleton<IChunkExecutionLimiter, ChunkExecutionLimiter>();
        services.AddSingleton<IMergeExecutionLimiter, MergeExecutionLimiter>();
        services.AddSingleton<IMergeBatchProgressReporterFactory, MergeBatchProgressReporterFactory>();
        services.AddSingleton<IInputFileAdapter, InputFileAdapter>();
        services.AddTransient<IRowParser, RowParser>();
        services.AddTransient<IComparer<Models.Item>, ItemComparer>();
        services.AddTransient<IItemFormatter, TextRowFormatter>();
        services.AddSingleton<IMergeBatchProcessor, MergeBatchProcessor>();
        services.AddSingleton<IMergeSortingCoordinator, MergeSortingCoordinator>();
        // TODO Binary file optimization
        //services.AddSingleton<BinaryRowFormatter>();
        services.AddTransient<IInputFileReader, InputFileReader>();
        services.AddSingleton<IChunkSorterFactory, ChunkSorterFactory>();
        services.AddSingleton<ISorterApplication, SorterApplication>();

        return services;
    }
}
