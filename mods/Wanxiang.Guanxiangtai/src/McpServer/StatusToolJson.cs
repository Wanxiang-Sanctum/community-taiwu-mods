using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class StatusToolJson
{
    internal sealed record Response(
        SideStatus Frontend,
        SideStatus Backend);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(AvailableStatus), "available")]
    [JsonDerivedType(typeof(UnavailableStatus), "unavailable")]
    internal abstract class SideStatus;

    internal sealed class AvailableStatus : SideStatus;

    internal sealed class UnavailableStatus(string reason) : SideStatus
    {
        public string Reason { get; } =
            string.IsNullOrWhiteSpace(reason)
                ? throw new ArgumentException("Unavailable reason is required.", nameof(reason))
                : reason;
    }
}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(StatusToolJson.Response))]
internal sealed partial class StatusToolJsonContext : JsonSerializerContext;
