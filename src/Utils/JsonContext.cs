using System.Text.Json;
using System.Text.Json.Serialization;
using Pixiv.Models.Novel;
using Pixiv.Models.Series;

namespace Pixiv.Utils;

// 统一的 JSON Source Generator 上下文 - 支持 AOT 和裁剪
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(NovelResponse))]
[JsonSerializable(typeof(NovelBody))]
[JsonSerializable(typeof(SeriesContentResponse))]
[JsonSerializable(typeof(SeriesBody))]
[JsonSerializable(typeof(ThumbnailCollection))]
[JsonSerializable(typeof(NovelInfo))]
[JsonSerializable(typeof(IReadOnlyList<NovelInfo>))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
internal partial class PixivJsonContext : JsonSerializerContext { }

