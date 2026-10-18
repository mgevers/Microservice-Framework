using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Common.Infrastructure.Compliance;

/// <summary>
/// Encrypts single fields, such as a name or an email, so that a reader of the database sees only ciphertext.
/// Transparent encryption at rest does not give this, because the database decrypts the data for every query.
/// </summary>
/// <remarks>
/// One master key (32 bytes) gives two keys through HKDF: one for AES-256-GCM and one for the blind index.
/// The format of a protected value is: 1 byte version, 12 bytes nonce, 16 bytes tag, then the ciphertext.
/// The version lets a later key or format coexist with old values.
/// The context (for example "account-id|email") is authenticated but not stored, so a value copied to another
/// row or field does not decrypt.
/// </remarks>
public sealed class FieldProtector
{
    public const string ConfigurationKey = "PiiEncryptionKey";

    private const byte Version = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] encryptionKey;
    private readonly byte[] indexKey;

    public FieldProtector(byte[] masterKey)
    {
        if (masterKey.Length != 32)
        {
            throw new ArgumentException("The master key must be 32 bytes.", nameof(masterKey));
        }

        encryptionKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, 32, info: Encoding.UTF8.GetBytes("field-encryption"));
        indexKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, 32, info: Encoding.UTF8.GetBytes("blind-index"));
    }

    /// <summary>The service does not start without the key, so it never stores readable personal data by mistake.</summary>
    public static FieldProtector FromConfiguration(IConfiguration configuration) =>
        new(KeySettings.GetKey(configuration, ConfigurationKey));

    public byte[] Protect(string plainText, string context)
    {
        var plain = Encoding.UTF8.GetBytes(plainText);
        var result = new byte[1 + NonceSize + TagSize + plain.Length];
        result[0] = Version;

        var nonce = result.AsSpan(1, NonceSize);
        var tag = result.AsSpan(1 + NonceSize, TagSize);
        var cipher = result.AsSpan(1 + NonceSize + TagSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(encryptionKey, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag, Encoding.UTF8.GetBytes(context));

        return result;
    }

    /// <exception cref="CryptographicException">The value is damaged, belongs to another context, or has an unknown version.</exception>
    public string Unprotect(byte[] protectedValue, string context)
    {
        if (protectedValue.Length < 1 + NonceSize + TagSize || protectedValue[0] != Version)
        {
            throw new CryptographicException("The protected value has an unknown format.");
        }

        var nonce = protectedValue.AsSpan(1, NonceSize);
        var tag = protectedValue.AsSpan(1 + NonceSize, TagSize);
        var cipher = protectedValue.AsSpan(1 + NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(encryptionKey, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain, Encoding.UTF8.GetBytes(context));

        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>
    /// A keyed hash that is the same for the same value. A database can enforce uniqueness and find a value with it,
    /// and cannot read the value. The caller normalizes the value first, for example lower case.
    /// </summary>
    public string BlindIndex(string normalizedValue) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(indexKey, Encoding.UTF8.GetBytes(normalizedValue)));
}
