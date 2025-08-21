using Common.Testing.Integration.Auth;
using Common.Testing.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;

namespace Common.Testing.Web;

public class WebAppFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILogger>();
            services.RemoveAll<ILoggerProvider>();

            services
                .ConfigureFakeJwtTokens()
                .ConfigureFakeApiKeys()
                .AddSingleton<IMessageSession, TestableMessageSession>();
        });

        builder.ConfigureLogging((WebHostBuilderContext context, ILoggingBuilder loggingBuilder) =>
        {
            loggingBuilder.SetupFakeLogging();
        });
    }
}
