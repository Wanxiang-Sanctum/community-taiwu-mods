using Newtonsoft.Json;

namespace Wanxiang.Taiwu.ModRpc;

internal static class WireProtocol
{
    internal const string ProtocolName = "Wanxiang.Taiwu.ModRpc.v1";

    internal const string RequestKind = "request";

    internal const string ResponseKind = "response";

    internal const string JsonPayloadKey = "json";

    internal const string ResponseMethodName = "Wanxiang.Taiwu.ModRpc.Response";

    internal static string SerializeRequest(
        string requestId,
        string methodName,
        string payloadJson,
        bool expectsResponse)
    {
        return JsonCodec.Serialize(new Envelope
        {
            Protocol = ProtocolName,
            Kind = RequestKind,
            RequestId = requestId,
            MethodName = methodName,
            PayloadJson = JsonPayload.Require(payloadJson, nameof(payloadJson)),
            ExpectsResponse = expectsResponse,
        });
    }

    internal static string SerializeResponse(
        string requestId,
        string payloadJson,
        string? error)
    {
        return JsonCodec.Serialize(new Envelope
        {
            Protocol = ProtocolName,
            Kind = ResponseKind,
            RequestId = requestId,
            PayloadJson = JsonPayload.Require(payloadJson, nameof(payloadJson)),
            Error = error,
        });
    }

    internal static string SerializeNativeResponse(string payloadJson, string? error)
    {
        return SerializeResponse(string.Empty, payloadJson, error);
    }

    internal static bool TryDeserializeEnvelope(
        string? json,
        out Envelope? envelope)
    {
        envelope = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            envelope = JsonCodec.Deserialize<Envelope>(json);
        }
        catch (JsonException)
        {
            return false;
        }

        if (envelope is null
            || !string.Equals(envelope.Protocol, ProtocolName, StringComparison.Ordinal)
            || !JsonPayload.TryNormalize(envelope.PayloadJson, out string payloadJson))
        {
            return false;
        }

        envelope.PayloadJson = payloadJson;
        return true;
    }

    internal static string ReadResponsePayload(string responseJson)
    {
        if (!TryDeserializeEnvelope(responseJson, out Envelope? response)
            || response is null
            || !string.Equals(response.Kind, ResponseKind, StringComparison.Ordinal))
        {
            throw new ModRpcException("ModRpc response is not a valid response envelope.");
        }

        if (!string.IsNullOrEmpty(response.Error))
        {
            throw new ModRpcException(response.Error);
        }

        return response.PayloadJson;
    }
}
