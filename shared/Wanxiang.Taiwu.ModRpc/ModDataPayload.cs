using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.ModRpc;

internal static class ModDataPayload
{
    internal static SerializableModData Create(string json)
    {
        SerializableModData payload = new();
        payload.Set(WireProtocol.JsonPayloadKey, JsonPayload.Require(json, nameof(json)));
        return payload;
    }

    internal static string Read(SerializableModData payload)
    {
        Guard.ThrowIfNull(payload, nameof(payload));

        if (!payload.Get(WireProtocol.JsonPayloadKey, out string json))
        {
            throw new ArgumentException(
                "ModRpc payload must contain JSON text in the internal json field.",
                nameof(payload));
        }

        return JsonPayload.Require(json, nameof(payload));
    }
}
