using System.Buffers.Text;
using DummyFile.Generator.Services.Abstract;

namespace DummyFile.Generator.Services;

public class RowFormatter : IRowFormatter
{
    public bool TryFormat(Item item, Span<byte> destination, out int written)
    {
        written = 0;

        if (!Utf8Formatter.TryFormat(item.Number, destination, out var numberWritten))
        {
            return false;
        }

        written += numberWritten;

        if (destination.Length < written + 2 + item.Text.Length + 1)
        {
            written = 0;
            return false;
        }

        destination[written++] = (byte)'.';
        destination[written++] = (byte)' ';

        item.TextSpan.CopyTo(destination[written..]);
        written += item.Text.Length;

        destination[written++] = (byte)'\n';
        return true;
    }
}
