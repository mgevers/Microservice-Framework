using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Common.Infrastructure.Email;

public interface IEmailSender
{
    /// <summary>Sends a plain text mail. It throws if the server does not accept it.</summary>
    Task Send(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task Send(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        if (!string.IsNullOrEmpty(settings.UserName))
        {
            await client.AuthenticateAsync(settings.UserName, settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

public static class EmailExtensions
{
    /// <summary>Registers the SMTP sender. The service does not start without a host and a From address.</summary>
    public static IServiceCollection AddSmtpEmail(this IServiceCollection services, Action<EmailOptions> configure)
    {
        var settings = new EmailOptions();
        configure(settings);
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.From))
        {
            throw new InvalidOperationException($"{nameof(EmailOptions)}:{nameof(EmailOptions.Host)} and {nameof(EmailOptions)}:{nameof(EmailOptions.From)} must be set.");
        }

        services.AddOptions<EmailOptions>().Configure(configure);

        return services.AddSingleton<IEmailSender, SmtpEmailSender>();
    }
}
