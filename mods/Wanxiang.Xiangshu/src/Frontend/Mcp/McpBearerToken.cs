using System.Net.Http.Headers;
using System.Security.Cryptography;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Mcp;

internal sealed class McpBearerToken
{
    private const int TokenByteLength = 32;

    private McpBearerToken(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public string AuthorizationHeaderValue =>
        new AuthenticationHeaderValue(IpcRuntime.BearerScheme, Value).ToString();

    public static McpBearerToken Create()
    {
        byte[] bytes = new byte[TokenByteLength];
        using RandomNumberGenerator generator = RandomNumberGenerator.Create();
        generator.GetBytes(bytes);

        return new McpBearerToken(ToBase64Url(bytes));
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
