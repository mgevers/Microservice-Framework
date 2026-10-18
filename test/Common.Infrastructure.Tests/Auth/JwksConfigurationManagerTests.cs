using System.Net;
using System.Security.Cryptography;
using Common.Infrastructure.Auth.Eos;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Common.Infrastructure.Tests.Auth;

public class JwksConfigurationManagerTests
{
    private sealed class SettableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 12, 8, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class KeyServer : HttpMessageHandler
    {
        private static readonly string Json = BuildJson();

        public int Requests;
        public bool Fail;

        private static string BuildJson()
        {
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(RSA.Create(2048)) { KeyId = "k1" });
            return $$"""{"keys":[{"kty":"RSA","use":"sig","kid":"k1","n":"{{jwk.N}}","e":"{{jwk.E}}"}]}""";
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Requests);

            return Task.FromResult(Fail
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Json) });
        }
    }

    private static (JwksConfigurationManager Manager, KeyServer Server, SettableClock Clock) Arrange()
    {
        var server = new KeyServer();
        var clock = new SettableClock();

        return (new JwksConfigurationManager(new HttpClient(server), "https://keys.test/jwks", clock), server, clock);
    }

    [Fact]
    public async Task TheKeysAreFetchedOnceAndKeptFor12Hours()
    {
        var (manager, server, clock) = Arrange();

        var first = await manager.GetConfigurationAsync(default);
        clock.Now += TimeSpan.FromHours(11);
        await manager.GetConfigurationAsync(default);

        Assert.Equal("k1", Assert.Single(first.SigningKeys).KeyId);
        Assert.Equal(1, server.Requests);

        clock.Now += TimeSpan.FromHours(2);
        await manager.GetConfigurationAsync(default);

        Assert.Equal(2, server.Requests);
    }

    [Fact]
    public async Task ARefreshRequest_RunsAtMostOnceInFiveMinutes()
    {
        var (manager, server, clock) = Arrange();
        await manager.GetConfigurationAsync(default);

        manager.RequestRefresh();
        clock.Now += TimeSpan.FromMinutes(1);
        await manager.GetConfigurationAsync(default);
        Assert.Equal(1, server.Requests);

        clock.Now += TimeSpan.FromMinutes(5);
        await manager.GetConfigurationAsync(default);
        Assert.Equal(2, server.Requests);
    }

    [Fact]
    public async Task IfARefreshFails_TheOldKeysStayInUse()
    {
        var (manager, server, clock) = Arrange();
        await manager.GetConfigurationAsync(default);
        server.Fail = true;

        clock.Now += TimeSpan.FromHours(13);
        var configuration = await manager.GetConfigurationAsync(default);

        Assert.Single(configuration.SigningKeys);
    }

    [Fact]
    public async Task IfTheFirstFetchFails_ThereAreNoKeys_AndTheFetchIsTriedAgainAfter30Seconds()
    {
        var (manager, server, clock) = Arrange();
        server.Fail = true;

        var first = await manager.GetConfigurationAsync(default);
        var second = await manager.GetConfigurationAsync(default);

        Assert.Empty(first.SigningKeys);
        Assert.Empty(second.SigningKeys);
        Assert.Equal(1, server.Requests);

        server.Fail = false;
        clock.Now += TimeSpan.FromSeconds(31);
        var third = await manager.GetConfigurationAsync(default);

        Assert.Single(third.SigningKeys);
        Assert.Equal(2, server.Requests);
    }
}
