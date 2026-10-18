using Microsoft.Extensions.Configuration;

namespace Common.Infrastructure.Compliance;

public static class MessageEncryption
{
    public const string ConfigurationKey = "MessageEncryptionKey";

    /// <summary>
    /// The AES-256 key (32 bytes, base64) that encrypts every message body on the bus.
    /// The service does not start without it, so it never sends readable messages by mistake.
    /// </summary>
    public static byte[] GetKey(IConfiguration configuration) => KeySettings.GetKey(configuration, ConfigurationKey);
}

public static class KeySettings
{
    /// <summary>Reads a 32 byte key, base64 encoded, from a setting. It throws if the setting is missing or has another length.</summary>
    public static byte[] GetKey(IConfiguration configuration, string name)
    {
        var value = configuration[name];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is not set. It must be a 32 byte key, base64 encoded.");
        }

        var key = Convert.FromBase64String(value);
        if (key.Length != 32)
        {
            throw new InvalidOperationException($"{name} must be 32 bytes, but it is {key.Length} bytes.");
        }

        return key;
    }
}
