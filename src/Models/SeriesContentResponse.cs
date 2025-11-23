namespace Pixiv.Models.Series;

public sealed record SeriesContentResponse(SeriesBody? Body);
public sealed record SeriesBody(ThumbnailCollection? Thumbnails);
public sealed record ThumbnailCollection(IReadOnlyList<NovelInfo>? Novel);
public sealed record NovelInfo(string? Id, string? Title);
