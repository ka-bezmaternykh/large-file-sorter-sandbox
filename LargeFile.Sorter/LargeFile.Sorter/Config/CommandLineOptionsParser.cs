namespace LargeFile.Sorter.Config;

public static class CommandLineOptionsParser
{
    public static bool TryParse(
        string[] args,
        out CommandLineOptions options,
        out string[] validationErrors)
    {
        options = new CommandLineOptions();
        var errors = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--help":
                    options.ShowHelp = true;
                    break;

                case "--force":
                    options.Force = true;
                    break;

                case "--file":
                    if (!TryReadValue(args, ref index, out var fileValue))
                    {
                        errors.Add("Missing value for --file");
                        continue;
                    }

                    options.File = fileValue;
                    break;

                case "--output-file":
                    if (!TryReadValue(args, ref index, out var outputFileValue))
                    {
                        errors.Add("Missing value for --output-file");
                        continue;
                    }

                    options.OutputFile = outputFileValue;
                    break;

                default:
                    errors.Add($"Unknown argument: {argument}");
                    break;
            }
        }

        if (!options.ShowHelp && string.IsNullOrWhiteSpace(options.File))
        {
            errors.Add("The --file option is required.");
        }

        if (!options.ShowHelp && string.IsNullOrWhiteSpace(options.OutputFile))
        {
            errors.Add("The --output-file option is required.");
        }

        validationErrors = errors.ToArray();
        return validationErrors.Length == 0;
    }

    public static string GetHelpText()
    {
        return """
               Usage:
                 LargeFile.Sorter [options]

               Options:
                 --file <path>         Path to the source file to sort.
                 --output-file <path>  Path to the output file for sorted data.
                 --force               Allow overwriting an existing output file.
                 --help                Show this help message.
               """;
    }

    private static bool TryReadValue(IReadOnlyList<string> args, ref int index, out string value)
    {
        var nextIndex = index + 1;
        if (nextIndex >= args.Count || args[nextIndex].StartsWith("--", StringComparison.Ordinal))
        {
            value = string.Empty;
            return false;
        }

        value = args[nextIndex];
        index = nextIndex;
        return true;
    }
}
