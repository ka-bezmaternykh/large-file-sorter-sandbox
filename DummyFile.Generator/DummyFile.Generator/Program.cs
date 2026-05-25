using DummyFile.Generator.Config;
using DummyFile.Generator.Services;
using DummyFile.Generator.Services.Abstract;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DummyFile.Generator;

class Program
{
    static async Task Main(string[] args)
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

        if (!FilesChecker.TryValidate(options, out validationErrors))
        {
            PrintValidationErrorsAndHelp(validationErrors);
            return;
        }

        Console.WriteLine("Command line options parsed successfully.");
        Console.WriteLine($"File: {options.File ?? "<not provided>"}");
        Console.WriteLine($"File size bytes: {options.FileSizeBytes?.ToString() ?? "<not provided>"}");
        Console.WriteLine($"File size lines: {options.FileSizeLines?.ToString() ?? "<not provided>"}");
        Console.WriteLine($"Force: {options.Force}");

        // Needed to handle Ctrl+C gracefully.
        using var cancellationTokenSource = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelKeyPressHandler = (_, eventArgs) =>
        {
            Console.WriteLine("Cancellation requested via Ctrl+C.");
            cancellationTokenSource.Cancel();
            eventArgs.Cancel = true;
        };

        Console.CancelKeyPress += cancelKeyPressHandler;
        try
        {
            await BootstrapAndRunAsync(options, cancellationTokenSource);
        }
        finally
        {
            // Small set of memory metrics in the end
            Console.WriteLine("Small set of memory metrics in the end");
            var process = Process.GetCurrentProcess();

            Console.WriteLine($"Peak working set: {process.PeakWorkingSet64 / 1024 / 1024} MB");
            Console.WriteLine($"Current working set: {process.WorkingSet64 / 1024 / 1024} MB");

            Console.CancelKeyPress -= cancelKeyPressHandler;
            if (Environment.UserInteractive && !Console.IsOutputRedirected && !Console.IsInputRedirected)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadKey(intercept: true);
            }
        }
    }

    private static async Task BootstrapAndRunAsync(CommandLineOptions options, CancellationTokenSource cancellationTokenSource)
    {
        #region Configure logging

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(console =>
            {
                console.SingleLine = true;
                console.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var programLogger = loggerFactory.CreateLogger<Program>();

        #endregion

        #region DI Section

        var itemsGenerator = new ItemsGenerator();
        var rowFormatter = new RowFormatter();
        var fileAdapterConfig = new FileAdapterConfig
        {
            FilePath = options.File!,
            IsOverwriteAllowed = options.Force
        };
        var generatorConfig = new GeneratorConfig
        {
            RequestedLinesNumber = options.FileSizeLines
        };
        var fileExporterConfig = new FileExporterConfig
        {
            MaxFileSize = options.FileSizeBytes
        };
        var fileAdapter = new FileAdapter(
            fileAdapterConfig,
            loggerFactory.CreateLogger<FileAdapter>());
        await using var fileExporter = new FileExporter(
            fileAdapter,
            loggerFactory.CreateLogger<FileExporter>(),
            fileExporterConfig);
        var generator = new Services.Generator(
            itemsGenerator,
            rowFormatter,
            fileExporter,
            loggerFactory.CreateLogger<Services.Generator>(),
            generatorConfig);

        #endregion

        programLogger.LogInformation("Starting file generation.");
        var startedAt = Stopwatch.GetTimestamp();
        await generator.RunAsync(cancellationTokenSource.Token);
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        if (cancellationTokenSource.IsCancellationRequested)
        {
            programLogger.LogInformation("File generation was canceled after {Elapsed}.", elapsed);
        }
        else
        {
            programLogger.LogInformation("File generation completed in {Elapsed}.", elapsed);
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
