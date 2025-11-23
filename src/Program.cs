using Serilog;
using System.Text;
using UserAgentGenerator;
using Pixiv.Models;
using Pixiv.Services;
using Pixiv.Utils;

class Program
{
  static async Task Main(string[] args)
  {
    // 設定 Serilog
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .MinimumLevel.Information()
        .CreateLogger();
    
    // 設定 Console 編碼
    Console.OutputEncoding = Encoding.UTF8;
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // 讀取設定檔
    var cfg = await ConfigLoader.LoadAsync(fileNameWithoutExt: "appsettings");
    var ua = UserAgent.Generate(Browser.Chrome, Platform.Mobile);
    var timeoutMs = ConfigLoader.GetInt(cfg, "DefaultTimeoutMs", 15000);
    var concurrency = ConfigLoader.GetInt(cfg, "Concurrency", 10);
    var outputDir = ConfigLoader.GetString(cfg, "OutputDir", "data");
    var cookie = ConfigLoader.GetString(cfg, "Cookie", string.Empty);
    var requestDelayMs = ConfigLoader.GetInt(cfg, "RequestDelayMs", 500);

    // 讀取小說 URL
    string homeUrl = "";
    while (string.IsNullOrWhiteSpace(homeUrl) || !homeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
    {
      Console.Write("請輸入小說 URL: ");
      homeUrl = (Console.ReadLine() ?? "").Trim();
      if (string.IsNullOrWhiteSpace(homeUrl) || !homeUrl.StartsWith("http"))
        Log.Error("❌ 只能輸入小說 URL");
    }
    // string homeUrl = "https://www.pixiv.net/novel/show.php?id=26333534";
    // string homeUrl = "https://www.pixiv.net/novel/series/11713692";

    Log.Information("正在抓取小說：{Url}", homeUrl);

    using var http = new HttpFetcher(ua, TimeSpan.FromMilliseconds(timeoutMs), cookie);
    // 建立小說爬蟲、章節提取器、下載器
    var scraper = new BookScraper(http);
    var extractor = new ChapterExtractor(http);
    var downloader = new Downloader(extractor);
    var isSeriesUrl = homeUrl.Contains("/novel/series/", StringComparison.OrdinalIgnoreCase);

    BookInfo book;
    try
    {
      // 批量抓取
      if (isSeriesUrl)
        book = await scraper.FetchBookInfoAsync(homeUrl);
      // 單章抓取
      else
        book = new BookInfo(
          readUrl: homeUrl,
          chapters: [new ChapterItem(Url: homeUrl)]
        );
    }
    catch (Exception ex)
    {
      Log.Fatal(ex, "抓取書籍資訊失敗");
      return;
    }

    // 打印章節資訊
    if (isSeriesUrl)
      for (int i = 0; i < book.Chapters.Count; i++)
      {
        var chapter = book.Chapters[i];
        Log.Information("第 {Index} 章：{Title}", i + 1, chapter.Title);
      }
    else
      Log.Information("章節：{Url}", book.ReadUrl);

    try
    {
      Log.Information("開始下載章節...");
      await downloader.DownloadChaptersAsync(
          book,
          outputDir,
          concurrency: concurrency,
          requestDelayMs: requestDelayMs
      );
      Log.Information("完成輸出。");
      // 按任意鍵退出
      Console.WriteLine("按任意鍵退出...");
      Console.ReadKey();
    }
    catch (Exception ex)
    {
      Log.Fatal(ex, "下載章節失敗");
    }
  }
}