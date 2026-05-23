using DummyFile.Generator.Config;

namespace DummyFile.Generator;

class Program
{
    static void Main(string[] args)
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
        Console.WriteLine($"File size lines: {options.FileSizeLines?.ToString() ?? "<not provided>"}");
        Console.WriteLine($"Force: {options.Force}");
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
