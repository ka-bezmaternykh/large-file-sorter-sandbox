using DummyFile.Generator.Services.Abstract;

namespace DummyFile.Generator.Services;

public sealed class ItemsGenerator : IItemsGenerator
{
    private const int MinWordLength = 3;
    private const int MaxWordLength = 12;
    private const int MinWordsPerText = 1;
    private const int MaxWordsPerText = 6;

    private readonly Random _random = new();

    public void Generate(out Item item)
    {
        var number = _random.Next(1, int.MaxValue);
        var text = CreateRandomText();

        item = new Item(number, text);
    }

    private ReadOnlyMemory<byte> CreateRandomText()
    {
        var wordCount = _random.Next(MinWordsPerText, MaxWordsPerText + 1);
        var totalLength = 0;

        Span<int> wordLengths = stackalloc int[MaxWordsPerText];
        for (var wordIndex = 0; wordIndex < wordCount; wordIndex++)
        {
            var wordLength = _random.Next(MinWordLength, MaxWordLength + 1);
            wordLengths[wordIndex] = wordLength;
            totalLength += wordLength;
        }

        totalLength += wordCount - 1;

        var bytes = new byte[totalLength];
        var position = 0;

        for (var wordIndex = 0; wordIndex < wordCount; wordIndex++)
        {
            if (wordIndex > 0)
            {
                bytes[position++] = (byte)' ';
            }

            for (var charIndex = 0; charIndex < wordLengths[wordIndex]; charIndex++)
            {
                bytes[position++] = (byte)_random.Next('a', 'z' + 1);
            }
        }

        return bytes.AsMemory();
    }
}
