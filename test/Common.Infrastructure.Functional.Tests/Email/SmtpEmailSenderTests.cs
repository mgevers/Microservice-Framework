using Common.Infrastructure.Email;
using Common.Testing.Integration.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Common.Infrastructure.Functional.Tests.Email;

public class SmtpEmailSenderTests(MailpitContainer mailpit) : IClassFixture<MailpitContainer>
{
    private SmtpEmailSender Sender() => new(Options.Create(new EmailOptions
    {
        Host = mailpit.Host,
        Port = mailpit.MappedSmtpPort,
        UseTls = false,
        From = "accounts@example.com",
    }));

    [Fact]
    public async Task Send_DeliversThePlainTextMailToTheServer()
    {
        await Sender().Send("mira@example.com", "Confirm", "The code is 1234.");

        var message = Assert.Single(await mailpit.Messages(), m => m.Subject == "Confirm");
        Assert.Equal("mira@example.com", message.To);
        Assert.Contains("The code is 1234.", message.Body);
    }

    [Fact]
    public async Task Send_ThrowsIfTheServerIsNotReachable()
    {
        var sender = new SmtpEmailSender(Options.Create(new EmailOptions { Host = "127.0.0.1", Port = 1, UseTls = false, From = "a@example.com" }));

        await Assert.ThrowsAnyAsync<Exception>(() => sender.Send("mira@example.com", "Confirm", "text"));
    }

    [Fact]
    public void AddSmtpEmail_RefusesToStartWithoutAHostOrAFromAddress()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddSmtpEmail(options => options.From = "a@example.com"));
        Assert.Throws<InvalidOperationException>(() => services.AddSmtpEmail(options => options.Host = "smtp"));
    }
}
