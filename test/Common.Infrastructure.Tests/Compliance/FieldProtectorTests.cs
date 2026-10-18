using System.Security.Cryptography;
using System.Text;
using Common.Infrastructure.Compliance;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Common.Infrastructure.Tests.Compliance;

public class FieldProtectorTests
{
    private static FieldProtector NewProtector(byte fill = 1) => new(Enumerable.Repeat(fill, 32).Select(b => b).ToArray());

    [Fact]
    public void Protect_ThenUnprotect_ReturnsTheValue_AndHidesItInTheCiphertext()
    {
        var protector = NewProtector();

        var cipher = protector.Protect("mira@example.com", "account-1|email");

        Assert.DoesNotContain("mira", Encoding.UTF8.GetString(cipher));
        Assert.Equal("mira@example.com", protector.Unprotect(cipher, "account-1|email"));
    }

    [Fact]
    public void Protect_TheSameValueTwice_GivesDifferentCiphertext()
    {
        var protector = NewProtector();

        Assert.NotEqual(protector.Protect("same", "c"), protector.Protect("same", "c"));
    }

    [Fact]
    public void Unprotect_WithAnotherContext_Throws()
    {
        var protector = NewProtector();
        var cipher = protector.Protect("value", "account-1|email");

        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(cipher, "account-2|email"));
    }

    [Fact]
    public void Unprotect_WithAnotherKey_OrDamagedData_Throws()
    {
        var cipher = NewProtector(1).Protect("value", "c");

        Assert.ThrowsAny<CryptographicException>(() => NewProtector(2).Unprotect(cipher, "c"));

        cipher[^1] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => NewProtector(1).Unprotect(cipher, "c"));
    }

    [Fact]
    public void Unprotect_WithAnUnknownVersion_Throws()
    {
        var protector = NewProtector();
        var cipher = protector.Protect("value", "c");
        cipher[0] = 99;

        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(cipher, "c"));
    }

    [Fact]
    public void BlindIndex_IsStable_KeyedAndDoesNotRevealTheValue()
    {
        var a = NewProtector(1);

        Assert.Equal(a.BlindIndex("mira@example.com"), a.BlindIndex("mira@example.com"));
        Assert.NotEqual(a.BlindIndex("mira@example.com"), a.BlindIndex("orin@example.com"));
        Assert.NotEqual(a.BlindIndex("mira@example.com"), NewProtector(2).BlindIndex("mira@example.com"));
        Assert.DoesNotContain("mira", a.BlindIndex("mira@example.com"));
    }

    [Fact]
    public void FromConfiguration_WithoutAKey_OrWithAWrongLength_Throws()
    {
        IConfiguration Config(string? value) => new ConfigurationBuilder()
            .AddInMemoryCollection(value is null ? [] : new Dictionary<string, string?> { [FieldProtector.ConfigurationKey] = value })
            .Build();

        Assert.Throws<InvalidOperationException>(() => FieldProtector.FromConfiguration(Config(null)));
        Assert.Throws<InvalidOperationException>(() => FieldProtector.FromConfiguration(Config(Convert.ToBase64String(new byte[16]))));
        Assert.NotNull(FieldProtector.FromConfiguration(Config(Convert.ToBase64String(new byte[32]))));
    }
}
