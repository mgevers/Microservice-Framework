using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Common.Infrastructure.Auth.Eos;

/// <summary>
/// Gives the signing keys of an issuer that publishes only a JWKS document, such as the Connect interface of EOS.
/// It keeps the keys for 12 hours. When a token has an unknown key id, the bearer handler asks for a refresh.
/// A refresh runs at most once in 5 minutes. If a refresh fails, the old keys stay in use.
/// If the first load fails, the manager gives no keys, so every token fails the check (the service fails closed), and it tries
/// again after 30 seconds.
/// </summary>
public sealed class JwksConfigurationManager(HttpClient http, string jwksUrl, TimeProvider? clock = null)
    : IConfigurationManager<OpenIdConnectConfiguration>
{
    private static readonly TimeSpan KeyLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan MinimumRefreshGap = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromSeconds(30);

    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private readonly SemaphoreSlim gate = new(1, 1);
    private OpenIdConnectConfiguration? current;
    private DateTimeOffset fetchedAt;
    private DateTimeOffset failedAt;
    private volatile bool refreshRequested;

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
    {
        var configuration = current;
        var started = time.GetUtcNow();
        if (configuration is not null && !IsStale(started))
        {
            return configuration;
        }

        if (configuration is null && failedAt != default && started - failedAt < RetryAfterFailure)
        {
            return new OpenIdConnectConfiguration();
        }

        await gate.WaitAsync(cancel);
        try
        {
            var now = time.GetUtcNow();
            if (current is not null && !IsStale(now))
            {
                return current;
            }

            try
            {
                current = await Fetch(cancel);
                fetchedAt = now;
                refreshRequested = false;
            }
            catch (Exception) when (current is not null)
            {
                // Keep the old keys and try again after the minimum gap.
                fetchedAt = now;
                refreshRequested = false;
            }
            catch (Exception) when (!cancel.IsCancellationRequested)
            {
                // No keys yet. Give none for now, so that no token passes.
                failedAt = now;
            }

            return current ?? new OpenIdConnectConfiguration();
        }
        finally
        {
            gate.Release();
        }
    }

    public void RequestRefresh() => refreshRequested = true;

    private bool IsStale(DateTimeOffset now)
    {
        var age = now - fetchedAt;

        return age >= KeyLifetime || (refreshRequested && age >= MinimumRefreshGap);
    }

    private async Task<OpenIdConnectConfiguration> Fetch(CancellationToken cancel)
    {
        var json = await http.GetStringAsync(jwksUrl, cancel);
        var keys = new JsonWebKeySet(json);
        var configuration = new OpenIdConnectConfiguration();
        foreach (var key in keys.GetSigningKeys())
        {
            configuration.SigningKeys.Add(key);
        }

        if (configuration.SigningKeys.Count == 0)
        {
            throw new InvalidOperationException($"The document at {jwksUrl} has no signing keys.");
        }

        return configuration;
    }
}
