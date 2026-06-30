using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Wanxiang.Guanxiangtai.McpServerRuntime;

namespace Wanxiang.Guanxiangtai.McpServer;

internal sealed class McpServerSettings
{
    private const int MinimumBearerTokenLength = 16;

    private const int GeneratedBearerTokenByteCount = 32;

    private const string LocalConfigurationFileName = "Guanxiangtai.Local.json";

    private McpServerSettings(
        string configurationPath,
        bool configurationFileExists,
        int port,
        string bearerToken,
        McpBearerTokenSource bearerTokenSource)
    {
        ConfigurationPath = configurationPath;
        ConfigurationFileExists = configurationFileExists;
        Port = port;
        BearerToken = bearerToken;
        BearerTokenSource = bearerTokenSource;
    }

    public string ConfigurationPath { get; }

    public bool ConfigurationFileExists { get; }

    public int Port { get; }

    public string BearerToken { get; }

    public McpBearerTokenSource BearerTokenSource { get; }

    public static string GetLocalConfigurationPath(string ownerDirectory)
    {
        ArgumentNullException.ThrowIfNull(ownerDirectory);

        if (string.IsNullOrWhiteSpace(ownerDirectory))
        {
            throw new ArgumentException("Runtime owner directory is required.", nameof(ownerDirectory));
        }

        return Path.Combine(
            Path.GetFullPath(ownerDirectory),
            LocalConfigurationFileName);
    }

    public static McpServerSettings Load(
        IConfiguration configuration,
        string configurationPath)
    {
        int port = configuration.GetValue<int?>("mcpServer:port") ?? 0;

        if (port is < 0 or > 65535)
        {
            throw new InvalidDataException(
                $"{LocalConfigurationFileName}: mcpServer.port must be between 0 and 65535.");
        }

        (string bearerToken, McpBearerTokenSource bearerTokenSource) = ReadBearerToken();

        return new McpServerSettings(
            configurationPath,
            File.Exists(configurationPath),
            port,
            bearerToken,
            bearerTokenSource);
    }

    private static (string Token, McpBearerTokenSource Source) ReadBearerToken()
    {
        string? token = Environment.GetEnvironmentVariable(
            GuanxiangtaiMcp.BearerTokenEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(token))
        {
            return (GenerateBearerToken(), McpBearerTokenSource.Generated);
        }

        string normalizedToken = token.Trim();

        if (normalizedToken.Length < MinimumBearerTokenLength)
        {
            throw new InvalidDataException(
                $"{GuanxiangtaiMcp.BearerTokenEnvironmentVariable} environment variable must contain at least {MinimumBearerTokenLength.ToString(System.Globalization.CultureInfo.InvariantCulture)} characters.");
        }

        return (normalizedToken, McpBearerTokenSource.Environment);
    }

    private static string GenerateBearerToken()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(GeneratedBearerTokenByteCount);

        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

internal enum McpBearerTokenSource
{
    Environment = 0,
    Generated = 1,
}
