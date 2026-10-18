using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Common.Testing.Integration.Containers;

/// <summary>Mailpit, the SMTP server that catches mail. It is the same image that docker compose uses.</summary>
public class MailpitContainer : IAsyncLifetime
{
    private const ushort SmtpPort = 1025;
    private const ushort ApiPort = 8025;

    private readonly HttpClient http = new();

    public IContainer Container { get; } = new ContainerBuilder("axllent/mailpit:v1.31.5")
        .WithPortBinding(SmtpPort, true)
        .WithPortBinding(ApiPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(ApiPort).ForPath("/readyz")))
        .Build();

    public string Host => Container.Hostname;

    public int MappedSmtpPort => Container.GetMappedPublicPort(SmtpPort);

    public Task InitializeAsync() => Container.StartAsync();

    public async Task DisposeAsync()
    {
        http.Dispose();
        await Container.StopAsync();
    }

    /// <summary>The mails that the server holds: the recipient, the subject and the text of each.</summary>
    public async Task<IReadOnlyList<(string To, string Subject, string Body)>> Messages()
    {
        var baseUrl = $"http://{Host}:{Container.GetMappedPublicPort(ApiPort)}/api/v1";
        var list = await http.GetFromJsonAsync<JsonNode>($"{baseUrl}/messages");

        var messages = new List<(string, string, string)>();
        foreach (var item in list!["messages"]!.AsArray())
        {
            var detail = await http.GetFromJsonAsync<JsonNode>($"{baseUrl}/message/{item!["ID"]}");
            messages.Add((detail!["To"]![0]!["Address"]!.GetValue<string>(), detail["Subject"]!.GetValue<string>(), detail["Text"]!.GetValue<string>()));
        }

        return messages;
    }
}
