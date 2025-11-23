using HtmlAgilityPack;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pixiv.Models;
using Pixiv.Utils;

namespace Pixiv.Services;

public sealed class BookScraper(HttpFetcher http)
{
  private readonly HttpFetcher _http = http;
  private const string BaseUrl = "https://www.pixiv.net";
  private const int SeriesPageLimit = 30;

  public async Task<BookInfo> FetchBookInfoAsync(string homeUrl, bool sortChapters = false)
  {
    var html = await _http.GetStringAsync(homeUrl);
    var doc = new HtmlDocument();
    doc.LoadHtml(html);

    // 挑選 meta 標籤
    string? PickMeta(params string[] selectors)
    {
      foreach (var s in selectors)
      {
        var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{s}']")
                   ?? doc.DocumentNode.SelectSingleNode($"//meta[@name='{s}']");
        var v = node?.GetAttributeValue("content", string.Empty);
        if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
      }
      return null;
    }

    var title = PickMeta("twitter:title", "og:title", "og:novel:book_name", "name");
    var author = ExtractAuthorFromTitle(PickMeta("og:title"));
    var description = PickMeta("description", "og:description");

    var chapters = await TryFetchSeriesChaptersAsync(homeUrl);
    var uniq = sortChapters ? DedupAndSortChapters(chapters) : chapters;

    return new BookInfo(title, author, description, readUrl: homeUrl, chapters: uniq);
  }

  private async Task<List<ChapterItem>> TryFetchSeriesChaptersAsync(string homeUrl)
  {
    var seriesId = ExtractSeriesId(homeUrl);
    if (string.IsNullOrWhiteSpace(seriesId))
      return [];

    var chapters = new List<ChapterItem>();
    var lastOrder = 0;
    while (true)
    {
      var apiUrl = $"{BaseUrl}/ajax/novel/series_content/{seriesId}?limit={SeriesPageLimit}&last_order={lastOrder}&order_by=asc&lang=zh_tw";
      var json = await _http.GetStringAsync(apiUrl, referer: homeUrl);
      var response = JsonSerializer.Deserialize(json, PixivJsonContext.Default.SeriesContentResponse);
      var novels = response?.Body?.Thumbnails?.Novel ?? [];

      if (novels.Count == 0)
        break;

      chapters.AddRange(novels
          .Select(n => new ChapterItem(n.Title?.Trim() ?? "Unknown Chapter", $"{BaseUrl}/novel/show.php?id={n.Id}")));

      if (novels.Count < SeriesPageLimit)
        break;

      lastOrder += SeriesPageLimit;
    }

    return chapters;
  }

  private static string? ExtractSeriesId(string homeUrl)
  {
    var match = Regex.Match(homeUrl, @"series/(\d+)");
    return match.Success ? match.Groups[1].Value : null;
  }

  private static string? ExtractAuthorFromTitle(string? title)
  {
    if (string.IsNullOrWhiteSpace(title)) return null;
    var match = Regex.Match(title, @"[/／]\s*([^/／]+?)的系列作品");
    if (!match.Success) return null;
    var author = match.Groups[1].Value.Trim();
    return author.Trim('「', '」');
  }

  // 去重、排序章節
  private static List<ChapterItem> DedupAndSortChapters(List<ChapterItem> chapters)
  {
    return [
        .. chapters
        .Select(ch => new ChapterItem(ch.Title, ch.Url))
    ];
  }
}