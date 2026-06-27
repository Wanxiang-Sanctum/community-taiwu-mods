using System.Diagnostics.CodeAnalysis;
using GameData.Domains.Item;
using Newtonsoft.Json;

namespace Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

internal static class GraftHostRpcProtocol
{
    private const int MinimumValidOwnerType = 1;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
    };

    public const string SubscribeHostMethodName = "Wanxiang.Taiwu.ItemGrafts.SubscribeHost";

    public const string UnsubscribeHostMethodName = "Wanxiang.Taiwu.ItemGrafts.UnsubscribeHost";

    public const string CreateHostMethodName = "Wanxiang.Taiwu.ItemGrafts.CreateHost";

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

    public static string CreateHostCreationPayload(
        GraftHostOwnerKey targetOwner,
        GraftHostTemplate hostTemplate)
    {
#if NET8_0
        ArgumentNullException.ThrowIfNull(hostTemplate);
#else
        if (hostTemplate is null)
        {
            throw new ArgumentNullException(nameof(hostTemplate));
        }
#endif

        return Serialize(new GraftHostCreationPayload
        {
            OwnerType = targetOwner.OwnerType,
            OwnerId = targetOwner.OwnerId,
            ItemType = hostTemplate.ItemType,
            TemplateId = hostTemplate.TemplateId,
        });
    }

    public static void ReadHostCreationRequest(
        string payloadJson,
        out GraftHostOwnerKey targetOwner,
        out GraftHostTemplate hostTemplate)
    {
        if (!TryDeserialize(payloadJson, out GraftHostCreationPayload? payload)
            || payload?.OwnerType is null
            || payload.OwnerId is null
            || payload.ItemType is null
            || payload.TemplateId is null
            || !IsSByte(payload.OwnerType.Value)
            || payload.OwnerType.Value < MinimumValidOwnerType)
        {
            throw new ArgumentException(
                "Payload does not contain a valid graft host creation request.",
                nameof(payloadJson));
        }

        try
        {
            targetOwner = new GraftHostOwnerKey(
                (sbyte)payload.OwnerType.Value,
                payload.OwnerId.Value);
            hostTemplate = new GraftHostTemplate(
                payload.ItemType.Value,
                payload.TemplateId.Value);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(
                "Payload does not contain a valid graft host creation request.",
                nameof(payloadJson),
                ex);
        }
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
            GraftHostOwnerChangedEventArgs ownerChanged => new GraftHostEventPayload
            {
                Kind = ownerChanged.Kind,
                HostKey = (ulong)ownerChanged.HostKey,
                FromOwnerType = GetOwnerType(ownerChanged.FromOwner),
                FromOwnerId = GetOwnerId(ownerChanged.FromOwner),
                ToOwnerType = GetOwnerType(ownerChanged.ToOwner),
                ToOwnerId = GetOwnerId(ownerChanged.ToOwner),
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
            return TryCreateHostEvent(payload, out hostEvent);
        }
        catch (ArgumentException)
        {
            return false;
        }
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

    private static bool TryCreateHostEvent(
        GraftHostEventPayload payload,
        [NotNullWhen(true)]
        out GraftHostEventArgs? hostEvent)
    {
        hostEvent = null;

        if (payload.Kind is null || payload.HostKey is null)
        {
            return false;
        }

        ItemKey hostKey = (ItemKey)payload.HostKey.Value;

        switch (payload.Kind.Value)
        {
            case GraftHostEventKind.Removed:
                hostEvent = GraftHostEventArgs.Removed(hostKey);
                return true;
            case GraftHostEventKind.OwnerChanged:
                return TryCreateOwnerChangedHostEvent(payload, hostKey, out hostEvent);
            case GraftHostEventKind.DataChanged:
                hostEvent = GraftHostEventArgs.DataChanged(hostKey);
                return true;
            default:
                return false;
        }
    }

    private static bool TryCreateOwnerChangedHostEvent(
        GraftHostEventPayload payload,
        ItemKey hostKey,
        [NotNullWhen(true)]
        out GraftHostEventArgs? hostEvent)
    {
        hostEvent = null;

        if (!TryReadOwner(payload.FromOwnerType, payload.FromOwnerId, out GraftHostOwnerKey? fromOwner)
            || !TryReadOwner(payload.ToOwnerType, payload.ToOwnerId, out GraftHostOwnerKey? toOwner)
            || (fromOwner is null && toOwner is null))
        {
            return false;
        }

        hostEvent = GraftHostEventArgs.OwnerChanged(
            hostKey,
            fromOwner,
            toOwner);
        return true;
    }

    private static bool TryReadOwner(
        int? ownerType,
        int? ownerId,
        out GraftHostOwnerKey? owner)
    {
        owner = null;

        if (ownerType is null && ownerId is null)
        {
            return true;
        }

        if (ownerType is null
            || ownerId is null
            || !IsSByte(ownerType.Value))
        {
            return false;
        }

        if (ownerType.Value < MinimumValidOwnerType)
        {
            return false;
        }

        owner = new GraftHostOwnerKey((sbyte)ownerType.Value, ownerId.Value);
        return true;
    }

    private static int? GetOwnerType(GraftHostOwnerKey? owner)
    {
        return owner.HasValue ? owner.Value.OwnerType : null;
    }

    private static int? GetOwnerId(GraftHostOwnerKey? owner)
    {
        return owner.HasValue ? owner.Value.OwnerId : null;
    }

    private static bool IsSByte(int value)
    {
        return value is >= sbyte.MinValue and <= sbyte.MaxValue;
    }

    private sealed class GraftHostPayload
    {
        public ulong? HostKey { get; set; }
    }

    private sealed class GraftHostCreationPayload
    {
        public int? OwnerType { get; set; }

        public int? OwnerId { get; set; }

        public sbyte? ItemType { get; set; }

        public short? TemplateId { get; set; }
    }

    private sealed class GraftHostEventPayload
    {
        public GraftHostEventKind? Kind { get; set; }

        public ulong? HostKey { get; set; }

        public int? FromOwnerType { get; set; }

        public int? FromOwnerId { get; set; }

        public int? ToOwnerType { get; set; }

        public int? ToOwnerId { get; set; }
    }

}
