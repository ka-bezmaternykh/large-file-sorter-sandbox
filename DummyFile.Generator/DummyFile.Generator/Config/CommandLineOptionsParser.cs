namespace DummyFile.Generator.Config;

public static class CommandLineOptionsParser
{
    private const long Megabyte = 1024L * 1024L;
    private const long Gigabyte = 1024L * 1024L * 1024L;

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

                case "--file-size-lines":
                    if (!TryReadValue(args, ref index, out var lineCountValue))
                    {
                        errors.Add("Missing value for --file-size-lines");
                        continue;
                    }

                    if (!int.TryParse(lineCountValue, out var lineCount) || lineCount <= 0)
                    {
                        errors.Add("The value for --file-size-lines must be a positive integer.");
                        continue;
                    }

                    options.FileSizeLines = lineCount;
                    break;

                case "--file-size":
                    if (!TryReadValue(args, ref index, out var fileSizeValue))
                    {
                        errors.Add("Missing value for --file-size");
                        continue;
                    }

                    if (!TryParseFileSize(fileSizeValue, out var fileSizeBytes))
                    {
                        errors.Add("The value for --file-size must match 1-999mb or 1-128gb.");
                        continue;
                    }

                    options.FileSizeBytes = fileSizeBytes;
                    break;

                default:
                    errors.Add($"Unknown argument: {argument}");
                    break;
            }
        }

        if (!options.ShowHelp && !options.FileSizeLines.HasValue && !options.FileSizeBytes.HasValue)
        {
            errors.Add("Either --file-size or --file-size-lines must be provided.");
        }

        validationErrors = errors.ToArray();
        return validationErrors.Length == 0;
    }

    public static string GetHelpText()
    {
        return """
               Usage:
                 DummyFile.Generator [options]

               Options:
                 --file <path>               Path to the file to generate or process.
                 --file-size <value>         Target file size in 1-999mb or 1-128gb format.
                 --file-size-lines <number>  Number of lines in the generated file.
                 --force                     Allow overwriting an existing file.
                 --help                      Show this help message.
               """;
    }

    private static bool TryParseFileSize(string value, out long fileSizeBytes)
    {
        fileSizeBytes = 0;

        if (value.EndsWith("mb", StringComparison.OrdinalIgnoreCase))
        {
            var sizePart = value[..^2];
            if (!int.TryParse(sizePart, out var sizeInMb) || sizeInMb < 1 || sizeInMb > 999)
            {
                return false;
            }

            fileSizeBytes = sizeInMb * Megabyte;
            return true;
        }

        if (value.EndsWith("gb", StringComparison.OrdinalIgnoreCase))
        {
            var sizePart = value[..^2];
            if (!int.TryParse(sizePart, out var sizeInGb) || sizeInGb < 1 || sizeInGb > 128)
            {
                return false;
            }

            fileSizeBytes = sizeInGb * Gigabyte;
            return true;
        }

        return false;
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
