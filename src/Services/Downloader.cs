using Serilog;
using System.Text;
using Pixiv.Models;

namespace Pixiv.Services;

public sealed class Downloader(ChapterExtractor extractor)
{
    private readonly ChapterExtractor _extractor = extractor;

    public async Task DownloadChaptersAsync(
        BookInfo bookInfo,
        string outputDir,
        int start = 0,
        int end = -1,
        int concurrency = 10,
        int requestDelayMs = 1000)
    {
        var allChapters = bookInfo.Chapters;
        if (allChapters.Count == 0)
        {
            Log.Warning("沒有章節可下載");
            return;
        }

        start = Math.Max(0, start);
        if (start >= allChapters.Count)
        {
            Log.Warning("start 已超過章節數 ({Start} >= {Count})", start, allChapters.Count);
            return;
        }

        if (end < 0 || end >= allChapters.Count)
            end = allChapters.Count - 1;
        else if (end < start)
            end = start;

        var chapters = allChapters.Skip(start).Take(end - start + 1).ToList();
        if (chapters.Count == 0)
        {
            Log.Warning("選定範圍內沒有章節 ({Start}~{End})", start, end);
            return;
        }

        var results = new string[chapters.Count];
        var chaptersTitles = new string[chapters.Count];
        var semaphore = new SemaphoreSlim(Math.Max(1, concurrency));
            var tasks = new List<Task>();

        for (int i = 0; i < chapters.Count; i++)
        {
            var idx = i;
            var ch = chapters[idx];
            var globalIndex = start + idx;
            string? referer =
                globalIndex == 0 ? bookInfo.ReadUrl : allChapters[globalIndex - 1].Url;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    if (requestDelayMs > 0)
                      await Task.Delay(requestDelayMs);
                    var result = await _extractor.FetchChapterContentAsync(ch.Url, referer: referer);
                    if (result is null)
                    {
                        Log.Warning("{Url} 章節內容為空: {Title}", ch.Url, ch.Title);
                        return;
                    }
                    var (content, chapterTitle) = result.Value;
                    chaptersTitles[idx] = chapterTitle;
                    var formattedContent = ChapterExtractor.BeautifyContent(content);
                    var displayTitle = chapterTitle ?? ch.Title;
                    results[idx] = $"【{displayTitle}】\n\n{formattedContent}\n";
                    Log.Information("{Done}/{Total} {Title}", globalIndex + 1, allChapters.Count, chaptersTitles[idx]);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{Done}/{Total} {Title} 抓取失敗", globalIndex + 1, allChapters.Count, ch.Title);
                    results[idx] = $"【{ch.Title}】（抓取失敗）{ch.Url}\n";
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        var isSeriesUrl = bookInfo.ReadUrl?.Contains("/novel/series/", StringComparison.OrdinalIgnoreCase) ?? false;

        // 組裝標頭
        var header = new List<string>();
        if (isSeriesUrl)
        {
            if (!string.IsNullOrWhiteSpace(bookInfo.Title)) header.Add($"書名：{bookInfo.Title}");
            if (!string.IsNullOrWhiteSpace(bookInfo.Author)) header.Add($"作者：{bookInfo.Author}");
            if (!string.IsNullOrWhiteSpace(bookInfo.Description)) header.Add($"簡介：{bookInfo.Description}");
            // 如果有標頭，則添加空行
            if (header.Count > 0) header.Add("");
        }

        // 組裝內容
        var finalText = string.Join("\n\n", header) + string.Join("\n\n\n\n", results);

        var safeName = isSeriesUrl ? bookInfo.Title.Trim() : chaptersTitles.FirstOrDefault() ?? bookInfo.Title.Trim();
        foreach (var c in Path.GetInvalidFileNameChars()) safeName = safeName.Replace(c, '_');

        var outDir = Path.Combine(outputDir);
        // 創建輸出目錄
        Directory.CreateDirectory(outDir);

        // 輸出
        var outPath = Path.Combine(outDir, $"{safeName}.txt");
        await File.WriteAllTextAsync(outPath, finalText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Log.Information("已輸出：{OutPath}", outPath);
    }
}