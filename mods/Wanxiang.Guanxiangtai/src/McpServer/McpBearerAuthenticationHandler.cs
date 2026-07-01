using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Wanxiang.Guanxiangtai.McpServerRuntime;

namespace Wanxiang.Guanxiangtai.McpServer;

internal static class McpBearerAuthenticationDefaults
{
    public const string AuthenticationScheme = "Wanxiang.Guanxiangtai.Bearer";
}

internal sealed class McpBearerAuthenticationOptions : AuthenticationSchemeOptions
{
    public string BearerToken { get; set; } = string.Empty;
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "ASP.NET Core authentication creates this handler through dependency injection.")]
internal sealed class McpBearerAuthenticationHandler(
    IOptionsMonitor<McpBearerAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<McpBearerAuthenticationOptions>(
        options,
        logger,
        encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!TryReadBearerToken(out string suppliedToken))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!FixedTimeEquals(suppliedToken, Options.BearerToken))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid bearer token."));
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, GuanxiangtaiMcp.ModId),
            new(ClaimTypes.Name, GuanxiangtaiMcp.DisplayName),
        ];
        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Task.CompletedTask;
    }

    private bool TryReadBearerToken(out string bearerToken)
    {
        bearerToken = string.Empty;

        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out Microsoft.Extensions.Primitives.StringValues values)
            || values.Count != 1)
        {
            return false;
        }

        if (!AuthenticationHeaderValue.TryParse(values[0], out AuthenticationHeaderValue? authorization)
            || !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authorization.Parameter))
        {
            return false;
        }

        bearerToken = authorization.Parameter.Trim();
        return true;
    }

    private static bool FixedTimeEquals(
        string suppliedToken,
        string expectedToken)
    {
        byte[] suppliedBytes = Encoding.UTF8.GetBytes(suppliedToken);
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        return CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }
}
