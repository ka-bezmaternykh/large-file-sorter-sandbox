namespace LargeFile.Sorter.Services.Models;

public readonly record struct MergeEntry(int SourceChunkNumber, Item Item);
