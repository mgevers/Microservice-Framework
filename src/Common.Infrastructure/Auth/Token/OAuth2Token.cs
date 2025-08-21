using Common.LanguageExtensions.TestableAlternatives;
using Newtonsoft.Json;

namespace Common.Infrastructure.Auth.Token;

public class OAuth2Token
{
    public OAuth2Token()
    {
        Issued = CurrentTime.UtcNow;
    }

    [JsonProperty("access_token")]
    public string? AccessToken { get; set; }

    [JsonProperty("token_type")]
    public string? TokenType { get; set; }

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonProperty("as:client_id")]
    public string? ClientId { get; set; }

    [JsonProperty("userName")]
    public string? UserName { get; set; }

    [JsonProperty("as:region")]
    public string? Region { get; set; }

    [JsonProperty(".issued")]
    public DateTime Issued { get; set; }

    [JsonProperty(".expires")]
    public DateTime Expires => Issued.AddSeconds(ExpiresIn);

    [JsonProperty("bearer")]
    public string? Bearer { get; set; }

}
