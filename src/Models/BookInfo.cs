namespace Pixiv.Models;

public sealed record BookInfo
{
    public string Title { get; init; }
    public string Author { get; init; }
    public string Description { get; init; }
    public string? ReadUrl { get; init; }

    public IReadOnlyList<ChapterItem> Chapters { get; init; }

    public BookInfo(
        string? title = null,
        string? author = null,
        string? description = null,
        string? readUrl = null,
        IReadOnlyList<ChapterItem>? chapters = null
    )
    {
        Title = title ?? "Unknown Title";
        Author = author ?? "Unknown Author";
        Description = description ?? string.Empty;
        ReadUrl = readUrl;
        Chapters = chapters ?? [];
    }
}