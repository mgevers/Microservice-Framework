namespace Common.Infrastructure.Auth.Token;

public class OAuthOptions
{
    public string Domain { get; set; } = null!;

    /// <summary>The full issuer URL. Set it for a provider such as Keycloak. When it is set, <see cref="Domain"/> is not used.</summary>
    public string? Issuer { get; set; }

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string Audience { get; set; } = null!;
}
