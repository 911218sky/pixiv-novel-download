using System.Text;
using System.Text.Json;
using Serilog;

namespace Pixiv.Utils;

public static class ConfigLoader
{
  private static readonly JsonSerializerOptions _jsonOptions = new()
  {
    // 允許 // 與 /* */ 註解
    ReadCommentHandling = JsonCommentHandling.Skip,
    // 允許尾逗號
    AllowTrailingCommas = true,
    // 忽略大小寫
    PropertyNameCaseInsensitive = true,
    // 使用 Source Generator 支持裁剪和 AOT
    TypeInfoResolver = PixivJsonContext.Default
  };

  /// <summary>
  /// 載入設定：優先 appsettings.jsonc，其次 appsettings.json
  /// </summary>
  public static async Task<Dictionary<string, JsonElement>> LoadAsync(
      string? baseDir = null,
      string? fileNameWithoutExt = "appsettings")
  {
    baseDir ??= AppContext.BaseDirectory;
    var jsonc = Path.Combine(baseDir, $"{fileNameWithoutExt}.jsonc");
    var json = Path.Combine(baseDir, $"{fileNameWithoutExt}.json");

    string? path = File.Exists(jsonc) ? jsonc :
                   File.Exists(json) ? json : null;

    if (path is not null)
      Log.Information("✔️ 載入設定檔：{Path}", path);
    else
      Log.Error("❌ 找不到設定檔使用預設值");

    if (path is null) return [];

    var text = await File.ReadAllTextAsync(path, Encoding.UTF8);
    return JsonSerializer.Deserialize(text, PixivJsonContext.Default.DictionaryStringJsonElement) ?? [];
  }

  // 取值工具
  public static string GetString(Dictionary<string, JsonElement> cfg, string key, string @default = "")
      => TryGet(cfg, key, out string? v) ? v! : @default;

  public static int GetInt(Dictionary<string, JsonElement> cfg, string key, int @default = 0)
      => TryGet(cfg, key, out int v) ? v : @default;

  public static bool GetBool(Dictionary<string, JsonElement> cfg, string key, bool @default = false)
      => TryGet(cfg, key, out bool v) ? v : @default;

  public static double GetDouble(Dictionary<string, JsonElement> cfg, string key, double @default = 0)
      => TryGet(cfg, key, out double v) ? v : @default;

  // 通用 TryGet
  [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "基本类型已通过上方 if 处理，泛型反序列化仅作后备")]
  public static bool TryGet<T>(Dictionary<string, JsonElement> cfg, string key, out T? value)
  {
    value = default;
    if (!cfg.TryGetValue(key, out var el)) return false;

    try
    {
      // 支援原生型別與數字/布林/字串轉換
      if (typeof(T) == typeof(string))
      {
        value = (T?)(object?)(el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString());
        return true;
      }
      if (typeof(T) == typeof(int) && el.TryGetInt32(out var i)) { value = (T?)(object)i; return true; }
      if (typeof(T) == typeof(long) && el.TryGetInt64(out var l)) { value = (T?)(object)l; return true; }
      if (typeof(T) == typeof(double) && el.TryGetDouble(out var d)) { value = (T?)(object)d; return true; }
      if (typeof(T) == typeof(bool) && el.ValueKind is JsonValueKind.True or JsonValueKind.False)
      {
        value = (T?)(object)el.GetBoolean(); return true;
      }
      // 其他型別走反序列化（很少使用，常用类型已在上方处理）
      #pragma warning disable IL2026
      value = el.Deserialize<T>(_jsonOptions);
      #pragma warning restore IL2026
      return value is not null;
    }
    catch
    {
      return false;
    }
  }
}
