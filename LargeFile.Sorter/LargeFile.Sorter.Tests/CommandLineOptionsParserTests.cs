using LargeFile.Sorter.Config;

namespace LargeFile.Sorter.Tests;

public class CommandLineOptionsParserTests
{
    [Fact]
    public void TryParse_ShouldReadFilePathOutputFileAndForce()
    {
        var success = CommandLineOptionsParser.TryParse(
            ["--file", "input.txt", "--output-file", "sorted.txt", "--force"],
            out var options,
            out var validationErrors);

        Assert.True(success);
        Assert.Empty(validationErrors);
        Assert.Equal("input.txt", options.File);
        Assert.Equal("sorted.txt", options.OutputFile);
        Assert.True(options.Force);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void TryParse_ShouldEnableHelpMode()
    {
        var success = CommandLineOptionsParser.TryParse(["--help"], out var options, out var validationErrors);

        Assert.True(success);
        Assert.Empty(validationErrors);
        Assert.True(options.ShowHelp);
        Assert.Null(options.File);
        Assert.Null(options.OutputFile);
        Assert.False(options.Force);
    }

    [Fact]
    public void TryParse_ShouldFailWhenFileValueIsMissing()
    {
        var success = CommandLineOptionsParser.TryParse(["--file"], out _, out var validationErrors);

        Assert.False(success);
        Assert.Equal(
            [
                "Missing value for --file",
                "The --file option is required.",
                "The --output-file option is required."
            ],
            validationErrors);
    }

    [Fact]
    public void TryParse_ShouldFailWhenOutputFileValueIsMissing()
    {
        var success = CommandLineOptionsParser.TryParse(
            ["--file", "input.txt", "--output-file"],
            out _,
            out var validationErrors);

        Assert.False(success);
        Assert.Equal(
            [
                "Missing value for --output-file",
                "The --output-file option is required."
            ],
            validationErrors);
    }

    [Fact]
    public void TryParse_ShouldFailWhenFileIsNotProvided()
    {
        var success = CommandLineOptionsParser.TryParse(
            ["--output-file", "sorted.txt"],
            out _,
            out var validationErrors);

        Assert.False(success);
        Assert.Equal(["The --file option is required."], validationErrors);
    }

    [Fact]
    public void TryParse_ShouldFailWhenOutputFileIsNotProvided()
    {
        var success = CommandLineOptionsParser.TryParse(
            ["--file", "input.txt"],
            out _,
            out var validationErrors);

        Assert.False(success);
        Assert.Equal(["The --output-file option is required."], validationErrors);
    }

    [Fact]
    public void TryParse_ShouldFailForUnknownArguments()
    {
        var success = CommandLineOptionsParser.TryParse(["--unknown"], out _, out var validationErrors);

        Assert.False(success);
        Assert.Equal(
            [
                "Unknown argument: --unknown",
                "The --file option is required.",
                "The --output-file option is required."
            ],
            validationErrors);
    }
}
