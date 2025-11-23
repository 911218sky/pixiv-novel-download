namespace Pixiv.Models.Novel;

public sealed record NovelResponse(NovelBody? Body);
public sealed record NovelBody(string? Title, string? Content);