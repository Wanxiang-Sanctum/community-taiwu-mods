using System.Diagnostics.CodeAnalysis;
using GameData.Domains.Item;
using Newtonsoft.Json;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

internal static class GraftHostRpcProtocol
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
    };

    public const string SubscribeHostMethodName = "Wanxiang.Taiwu.ItemGrafts.SubscribeHost";

    public const string UnsubscribeHostMethodName = "Wanxiang.Taiwu.ItemGrafts.UnsubscribeHost";

    public const string HostEventMethodName = "Wanxiang.Taiwu.ItemGrafts.HostEvent";

    public const string NullPayload = "null";

    public static string CreateHostPayload(ItemKey hostKey)
    {
        ItemKey validatedHostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));

        return Serialize(new GraftHostPayload
        {
            HostKey = (ulong)validatedHostKey,
        });
    }

    public static ItemKey ReadHostKey(string payloadJson)
    {
        if (!TryReadHostKey(payloadJson, out ItemKey hostKey))
        {
            throw new ArgumentException("Payload does not contain a valid graft host key.", nameof(payloadJson));
        }

        return hostKey;
    }

    private static bool TryReadHostKey(string payloadJson, out ItemKey hostKey)
    {
        hostKey = ItemKey.Invalid;

        if (!TryDeserialize(payloadJson, out GraftHostPayload? payload)
            || payload?.HostKey is null)
        {
            return false;
        }

        ItemKey parsedHostKey = (ItemKey)payload.HostKey.Value;

        if (!GraftHostValidation.IsValidKey(parsedHostKey))
        {
            return false;
        }

        hostKey = parsedHostKey;
        return true;
    }

    public static string SerializeHostEvent(GraftHostEventArgs hostEvent)
    {
#if NET8_0
        ArgumentNullException.ThrowIfNull(hostEvent);
#else
        if (hostEvent is null)
        {
            throw new ArgumentNullException(nameof(hostEvent));
        }
#endif

        GraftHostEventPayload payload = hostEvent switch
        {
            GraftHostRemovedEventArgs removed => new GraftHostEventPayload
            {
                Kind = removed.Kind,
                HostKey = (ulong)removed.HostKey,
            },
            GraftHostLocationChangedEventArgs locationChanged => new GraftHostEventPayload
            {
                Kind = locationChanged.Kind,
                HostKey = (ulong)locationChanged.HostKey,
                FromCharacterId = locationChanged.FromCharacterId,
                ToCharacterId = locationChanged.ToCharacterId,
            },
            GraftHostDataChangedEventArgs dataChanged => new GraftHostEventPayload
            {
                Kind = dataChanged.Kind,
                HostKey = (ulong)dataChanged.HostKey,
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(hostEvent),
                hostEvent.GetType(),
                "Unsupported graft host event type."),
        };

        return Serialize(payload);
    }

    public static bool TryDeserializeHostEvent(
        string payloadJson,
        [NotNullWhen(true)]
        out GraftHostEventArgs? hostEvent)
    {
        hostEvent = null;

        if (!TryDeserialize(payloadJson, out GraftHostEventPayload? payload)
            || payload?.Kind is null)
        {
            return false;
        }

        try
        {
            hostEvent = payload.Kind.Value switch
            {
                GraftHostEventKind.Removed when payload.HostKey is not null =>
                    GraftHostEventArgs.Removed((ItemKey)payload.HostKey.Value),
                GraftHostEventKind.LocationChanged
                    when payload.HostKey is not null
                        && (payload.FromCharacterId is not null || payload.ToCharacterId is not null) =>
                    GraftHostEventArgs.LocationChanged(
                        (ItemKey)payload.HostKey.Value,
                        payload.FromCharacterId,
                        payload.ToCharacterId),
                GraftHostEventKind.DataChanged when payload.HostKey is not null =>
                    GraftHostEventArgs.DataChanged((ItemKey)payload.HostKey.Value),
                _ => null,
            };
        }
        catch (ArgumentException)
        {
            return false;
        }

        return hostEvent is not null;
    }

    private static string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, JsonSettings);
    }

    private static bool TryDeserialize<T>(string json, out T? value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed class GraftHostPayload
    {
        public ulong? HostKey { get; set; }
    }

    private sealed class GraftHostEventPayload
    {
        public GraftHostEventKind? Kind { get; set; }

        public ulong? HostKey { get; set; }

        public int? FromCharacterId { get; set; }

        public int? ToCharacterId { get; set; }
    }

}
