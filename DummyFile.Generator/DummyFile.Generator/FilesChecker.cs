using DummyFile.Generator.Config;

namespace DummyFile.Generator;

public static class FilesChecker
{
    public static bool TryValidate(CommandLineOptions options, out string[] validationErrors)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.File) && File.Exists(options.File) && !options.Force)
        {
            errors.Add("The file already exists. Use --force to overwrite it.");
        }

        validationErrors = errors.ToArray();
        return validationErrors.Length == 0;
    }
}
