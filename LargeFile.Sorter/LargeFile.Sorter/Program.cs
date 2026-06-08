using LargeFile.Sorter.Config;
using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter;

public static class Program
{
    public static async Task Main(string[] args)
    {
        try
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

            await application.RunAsync();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            Environment.ExitCode = 1;
        }
        finally
        {
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(intercept: true);
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
