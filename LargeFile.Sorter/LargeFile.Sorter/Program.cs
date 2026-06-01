using LargeFile.Sorter.Config;
using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace LargeFile.Sorter;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (!CommandLineOptionsParser.TryParse(args, out var options, out var validationErrors))
        {
            PrintValidationErrorsAndHelp(validationErrors);
            return;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(CommandLineOptionsParser.GetHelpText());
            return;
        }

        using var host = AppHost.Build(args, options);
        var application = host.Services.GetRequiredService<ISorterApplication>();

        try
        {
            await application.RunAsync();
        }
        finally
        {
            Console.WriteLine("Small set of memory metrics in the end");
            var process = Process.GetCurrentProcess();

            Console.WriteLine($"Peak working set: {process.PeakWorkingSet64 / 1024 / 1024} MB");
            Console.WriteLine($"Current working set: {process.WorkingSet64 / 1024 / 1024} MB");
        }
    }

    private static void PrintValidationErrorsAndHelp(IEnumerable<string> validationErrors)
    {
        foreach (var validationError in validationErrors)
        {
            Console.Error.WriteLine(validationError);
        }

        Console.Error.WriteLine();
        Console.WriteLine(CommandLineOptionsParser.GetHelpText());
        Environment.ExitCode = 1;
    }
}
