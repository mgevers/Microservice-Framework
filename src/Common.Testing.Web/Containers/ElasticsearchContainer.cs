using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Testcontainers.Elasticsearch;
using Xunit;

namespace Common.Testing.Integration.Containers;

public class ElasticsearchContainer : IAsyncLifetime
{
    public Testcontainers.Elasticsearch.ElasticsearchContainer Container { get; } = new ElasticsearchBuilder()
        .WithImage("elasticsearch:8.17.2")
        .WithPortBinding(9200, true)
        .WithEnvironment("discovery.type", "single-node")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithEnvironment("xpack.security.http.ssl.enabled", "false")
        .Build();

    public ElasticsearchClient ElasticsearchClient => GetElasticsearchClient();

    public Task InitializeAsync()
    {
        return Container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Container.StopAsync();
    }

    private ElasticsearchClient GetElasticsearchClient()
    {
        var connectionString = Container.GetConnectionString().Replace("https", "http");
        var settings = new ElasticsearchClientSettings(new Uri(connectionString))
            .EnableDebugMode();
        settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);
        settings.DisableDirectStreaming();

        return new ElasticsearchClient(settings);
    }
}
