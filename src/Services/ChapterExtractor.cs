using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Serilog;
using Pixiv.Utils;

namespace Pixiv.Services;

public sealed class ChapterExtractor(HttpFetcher http)
{
    private readonly HttpFetcher _http = http;
    private const string Encoding = "utf-8";
    private const string BaseUrl = "https://www.pixiv.net";

    public static string BeautifyContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var builder = new StringBuilder();
        foreach (
            var line in content
                     .Split('\n')
                     .Select(line => line.Trim())
                     .Where(line => line.Length > 0)
        )
        {
            builder.AppendLine(line);
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    public async Task<(string content, string title)?> FetchChapterContentAsync(string url, string? referer = null)
    {
        var novelId = ExtractNovelId(url);
        if (string.IsNullOrWhiteSpace(novelId))
            throw new InvalidOperationException("找不到 novelId");
        var apiUrl = $"{BaseUrl}/ajax/novel/{novelId}?lang=zh_tw";
        var json = await _http.GetStringAsync(apiUrl, referer: referer, encoding: Encoding);
        var response = JsonSerializer.Deserialize(json, PixivJsonContext.Default.NovelResponse);

        var content = response?.Body?.Content;
        var title = response?.Body?.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            Log.Warning("{Url} 章節內容為空: {Title}", url, title);
            return null;
        }
        return (content, title);
    }

    private static string? ExtractNovelId(string url)
    {
        var match = Regex.Match(url, @"series/(\d+)");
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(url, @"[?&]id=(\d+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    // 從 readUrl 中提取 chapterId
    public static int GetChapterIdFromReadUrl(string readUrl)
    {
        var u = new Uri(readUrl);
        var q = u.Query.TrimStart('?');
        var idStr = Regex.Replace(q.Split('_')[0], @"[^\d]", "");
        if (!int.TryParse(idStr, out var id)) throw new InvalidOperationException("chapterId 解析失敗");
        return id;
    }
}