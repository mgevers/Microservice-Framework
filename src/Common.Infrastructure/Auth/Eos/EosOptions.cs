namespace Common.Infrastructure.Auth.Eos;

/// <summary>
/// The settings for players who sign in with Epic Online Services (EOS). The game client gets an ID token from
/// <c>EOS_Connect_CopyIdToken</c> and sends it as a bearer token. The service checks it against the public keys of Epic.
/// EOS is off until <see cref="Audience"/> is set.
/// </summary>
public class EosOptions
{
    public const string DefaultIssuerBase = "https://api.epicgames.dev";
    public const string DefaultJwksUrl = "https://api.epicgames.dev/auth/v1/oauth/jwks";
    public const string DefaultSubjectClaim = "sub";

    /// <summary>The audience that the token must have. It is the client id of the game in the EOS Developer Portal. Empty turns EOS off.</summary>
    public string? Audience { get; set; }

    /// <summary>The base URL of the issuer. The <c>iss</c> claim must be this URL, or start with it and a slash.</summary>
    public string? IssuerBase { get; set; }

    /// <summary>Where the public keys of Epic are. A test points it at a stub.</summary>
    public string? JwksUrl { get; set; }

    /// <summary>The claim that holds the id of the player, the Product User ID. The service copies it to <c>sub</c> if the name differs.</summary>
    public string? SubjectClaim { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(Audience);

    // An empty setting means the default, because a settings source can send an empty text for a key that is not set.
    public string EffectiveIssuerBase => Or(IssuerBase, DefaultIssuerBase).TrimEnd('/');

    public string EffectiveJwksUrl => Or(JwksUrl, DefaultJwksUrl);

    public string EffectiveSubjectClaim => Or(SubjectClaim, DefaultSubjectClaim);

    /// <summary>True if the issuer is the base URL or a path below it. A host that only starts with the same text does not pass.</summary>
    public bool IsIssuer(string? issuer) =>
        issuer is not null
        && (issuer.Equals(EffectiveIssuerBase, StringComparison.OrdinalIgnoreCase)
            || issuer.StartsWith(EffectiveIssuerBase + "/", StringComparison.OrdinalIgnoreCase));

    private static string Or(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
