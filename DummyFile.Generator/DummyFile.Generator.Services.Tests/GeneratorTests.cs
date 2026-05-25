using DummyFile.Generator.Services.Abstract;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace DummyFile.Generator.Services.Tests;

public class GeneratorTests
{
    [Fact]
    public async Task RunAsync_ShouldRespectMaxLines()
    {
        var itemsGenerator = new FakeItemsGenerator();
        var rowFormatter = new FakeRowFormatter();
        var fileExporter = new FakeFileExporter();
        var generator = new Generator(
            itemsGenerator,
            rowFormatter,
            fileExporter,
            NullLogger<Generator>.Instance,
            new GeneratorConfig
            {
                RequestedLinesNumber = 3
            });

        await generator.RunAsync();

        Assert.Equal(3, fileExporter.AcceptedRows.Count);
    }

    [Fact]
    public async Task RunAsync_ShouldSkipRowsWhenFormatterFails()
    {
        var itemsGenerator = new FakeItemsGenerator();
        var rowFormatter = new FakeRowFormatter(failOnCallNumbers: [2, 4]);
        var fileExporter = new FakeFileExporter();
        var generator = new Generator(
            itemsGenerator,
            rowFormatter,
            fileExporter,
            NullLogger<Generator>.Instance,
            new GeneratorConfig
            {
                RequestedLinesNumber = 4
            });

        await generator.RunAsync();

        Assert.Equal(4, fileExporter.AcceptedRows.Count);
        Assert.True(rowFormatter.CallCount > fileExporter.AcceptedRows.Count);
    }

    [Fact]
    public async Task RunAsync_ShouldStopWhenCancellationIsRequested()
    {
        var itemsGenerator = new FakeItemsGenerator();
        var rowFormatter = new FakeRowFormatter();
        using var cancellationTokenSource = new CancellationTokenSource();
        var fileExporter = new FakeFileExporter(onExport: () => cancellationTokenSource.Cancel());
        var generator = new Generator(
            itemsGenerator,
            rowFormatter,
            fileExporter,
            NullLogger<Generator>.Instance,
            new GeneratorConfig());

        await generator.RunAsync(cancellationTokenSource.Token);

        Assert.Single(fileExporter.AcceptedRows);
    }

    private sealed class FakeItemsGenerator : IItemsGenerator
    {
        private int _number = 1;

        public void Generate(out Item item)
        {
            item = new Item(_number++, "row"u8.ToArray());
        }
    }

    private sealed class FakeRowFormatter : IRowFormatter
    {
        private readonly HashSet<int> _failOnCallNumbers;
        private int _callNumber;

        public FakeRowFormatter(IEnumerable<int>? failOnCallNumbers = null)
        {
            _failOnCallNumbers = failOnCallNumbers is null
                ? []
                : new HashSet<int>(failOnCallNumbers);
        }

        public int CallCount => _callNumber;

        public bool TryFormat(Item item, Span<byte> destination, out int written)
        {
            _callNumber++;

            if (_failOnCallNumbers.Contains(_callNumber))
            {
                written = 0;
                return false;
            }

            var content = Encoding.ASCII.GetBytes($"{item.Number}. ok\n");
            content.CopyTo(destination);
            written = content.Length;
            return true;
        }
    }

    private sealed class FakeFileExporter : IFileExporter
    {
        private readonly Action? _onExport;

        public FakeFileExporter(Action? onExport = null)
        {
            _onExport = onExport;
        }

        public List<byte[]> AcceptedRows { get; } = [];

        public ValueTask<bool> TryExportAsync(ReadOnlyMemory<byte> row, CancellationToken cancellationToken = default)
        {
            AcceptedRows.Add(row.ToArray());
            _onExport?.Invoke();
            return ValueTask.FromResult(true);
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
