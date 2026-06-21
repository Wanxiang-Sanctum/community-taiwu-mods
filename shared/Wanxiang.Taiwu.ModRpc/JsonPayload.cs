using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wanxiang.Taiwu.ModRpc;

internal static class JsonPayload
{
    internal const string NullJson = "null";

    internal static string Require(string payloadJson, string parameterName)
    {
        if (payloadJson is null)
        {
            throw new ArgumentNullException(
                parameterName,
                "Payload must be JSON text. Use \"null\" for JSON null.");
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ArgumentException("Payload JSON must not be empty.", parameterName);
        }

        try
        {
            _ = JToken.Parse(payloadJson);
        }
        catch (JsonReaderException ex)
        {
            throw new ArgumentException("Payload must be valid JSON text.", parameterName, ex);
        }

        return payloadJson;
    }

    internal static bool TryNormalize(string? payloadJson, out string normalizedPayloadJson)
    {
        if (payloadJson is null)
        {
            normalizedPayloadJson = string.Empty;
            return false;
        }

        try
        {
            normalizedPayloadJson = Require(payloadJson, nameof(payloadJson));
            return true;
        }
        catch (ArgumentException)
        {
            normalizedPayloadJson = string.Empty;
            return false;
        }
    }
}
