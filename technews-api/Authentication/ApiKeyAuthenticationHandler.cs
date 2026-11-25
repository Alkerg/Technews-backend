using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace technews_api.Authentication
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string HeaderName { get; set; } = "X-Api-Key";
        public string ApiKey { get; set; } = "";
    }

    public class ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
    {
        public const string SchemeName = "ApiKey";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Options.HeaderName, out var provided) || provided.Count != 1)
                return Task.FromResult(AuthenticateResult.NoResult());

            var expected = SHA256.HashData(Encoding.UTF8.GetBytes(Options.ApiKey));
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString()));

            if (!CryptographicOperations.FixedTimeEquals(expected, actual))
                return Task.FromResult(AuthenticateResult.Fail("API key inválida."));

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "n8n-ingest")], SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
