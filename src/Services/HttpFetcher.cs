using Polly;
using Polly.Retry;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Pixiv.Services;

public sealed class HttpFetcher : IDisposable
{
  private readonly HttpClient _http;
  private readonly AsyncRetryPolicy<HttpResponseMessage> _retry;
  private readonly string? _cookieHeader;

  public HttpFetcher(string userAgent, TimeSpan timeout, string? cookieHeader = null)
  {
    _cookieHeader = cookieHeader;
    var handler = new HttpClientHandler
    {
      // 自動解壓縮 GZip 和 Deflate 壓縮
      AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };
    _http = new HttpClient(handler)
    {
      Timeout = timeout
    };
    _http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-TW,zh;q=0.9,en;q=0.8");

    // 設定重試策略
    _retry = Policy<HttpResponseMessage>
        // 處理 HttpRequestException 和 HTTP 狀態碼不是 200 或 >= 400 的結果
        .Handle<HttpRequestException>()
        .OrResult(r => (int)r.StatusCode is < 200 or >= 400)
        // 重試3次，每次間隔500ms、1000ms、2000ms
        .WaitAndRetryAsync(
        [
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(1000),
            TimeSpan.FromMilliseconds(2000)
        ], (res, ts, retry, _) =>
        {
          Log.Warning("HTTP 重試第 {Retry} 次（{Delay}）：{Info}",
                  retry, ts, res.Exception?.Message ?? res.Result.StatusCode.ToString());
        });
  }

  // 取得字串
  public async Task<string> GetStringAsync(
    string url,
    string? referer = null,
    string encoding = "utf-8"
  )
  {
    using var req = new HttpRequestMessage(HttpMethod.Get, url);

    if (!string.IsNullOrWhiteSpace(_cookieHeader))
      req.Headers.TryAddWithoutValidation("Cookie", _cookieHeader);

    // 如有 referer，設定 Referrer
    if (!string.IsNullOrWhiteSpace(referer))
      req.Headers.Referrer = new Uri(referer);

    // 發送請求
    var res = await _retry.ExecuteAsync(() => _http.SendAsync(req));

    // 確保成功
    res.EnsureSuccessStatusCode();

    var bytes = await res.Content.ReadAsByteArrayAsync();
    var enc = Encoding.GetEncoding(encoding);
    return enc.GetString(bytes);
  }

  // 釋放資源
  public void Dispose() => _http.Dispose();
}
