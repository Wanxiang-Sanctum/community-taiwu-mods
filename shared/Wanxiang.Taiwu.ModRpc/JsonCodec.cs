using GameData.Domains.Mod;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wanxiang.Taiwu.ModRpc;

internal static class JsonCodec
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
    };

    internal static string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, JsonSettings);
    }

    internal static T? Deserialize<T>(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonConvert.DeserializeObject<T>(json, JsonSettings);
    }

    internal static string SerializePayload<T>(T value)
    {
        ThrowIfNativePayload<T>();
        ThrowIfNativePayload(value);
        string? json = JsonConvert.SerializeObject(value, JsonSettings);
        return JsonPayload.Require(json ?? JsonPayload.NullJson, nameof(value));
    }

    internal static T DeserializePayload<T>(string json)
    {
        ThrowIfNativePayload<T>();
        string payloadJson = JsonPayload.Require(json, nameof(json));

        T? value;

        try
        {
            if (JToken.Parse(payloadJson).Type == JTokenType.Null)
            {
                if (default(T) is null)
                {
                    return default!;
                }

                throw new ModRpcException(
                    $"Payload JSON null cannot be deserialized as '{typeof(T).FullName}'.");
            }

            value = JsonConvert.DeserializeObject<T>(payloadJson, JsonSettings);
        }
        catch (JsonException ex)
        {
            throw new ModRpcException(
                $"Payload JSON cannot be deserialized as '{typeof(T).FullName}'.",
                ex);
        }

        if (value is null && default(T) is not null)
        {
            throw new ModRpcException(
                $"Payload JSON cannot be deserialized as '{typeof(T).FullName}'.");
        }

        return value!;
    }

    private static void ThrowIfNativePayload<T>(T value)
    {
        if (value is SerializableModData)
        {
            throw new NotSupportedException(
                "SerializableModData is not supported by ModRpc public API. Convert it to a stable JSON DTO.");
        }
    }

    private static void ThrowIfNativePayload<T>()
    {
        if (typeof(T) == typeof(SerializableModData))
        {
            throw new NotSupportedException(
                "SerializableModData is not supported by ModRpc public API. Convert it to a stable JSON DTO.");
        }
    }
}
