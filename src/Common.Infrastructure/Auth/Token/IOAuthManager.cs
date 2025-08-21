namespace Common.Infrastructure.Auth.Token;

public interface IOAuthManager
{
    Task<OAuth2Token?> GetOAuth2Token(OAuthOptions options);
}
