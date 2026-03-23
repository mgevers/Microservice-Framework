using Common.LanguageExtensions.Contracts;
using Common.Testing.Integration.Chaos;
using Common.Testing.Persistence;
using Common.Testing.ServiceBus;
using Common.Testing.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TestApp.Core.CommandHandlers;
using TestApp.Core.Domain;

namespace TestApp.Application.Api.Integration.Tests;

public class TestAppWebApplicationFactory : WebAppFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services
                .AddSingleton<IRepository<Character>, FakeRepository<Character>>()
                .AddSingleton<IMessageSession, FakeMessageSession>()
                .AddMediatRChaos();
        });
    }
}
