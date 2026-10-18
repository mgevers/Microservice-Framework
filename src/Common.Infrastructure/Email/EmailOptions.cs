namespace Common.Infrastructure.Email;

/// <summary>
/// The SMTP server that a service sends mail through. The Docker stack uses Mailpit, which catches the mail.
/// A real environment sets a provider, with <see cref="UseTls"/> on.
/// </summary>
public class EmailOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>STARTTLS. Only a local test server, such as Mailpit, runs with this off.</summary>
    public bool UseTls { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    /// <summary>The address in the From header.</summary>
    public string From { get; set; } = string.Empty;
}
