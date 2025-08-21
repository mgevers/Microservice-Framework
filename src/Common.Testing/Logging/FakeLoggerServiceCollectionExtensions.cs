using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Testing.Logging;

public static class FakeLoggerServiceCollectionExtensions
{
    public static ILoggingBuilder SetupFakeLogging(this ILoggingBuilder builder)
    {

        builder.ClearProviders();
        builder.Services.AddSingleton<ILoggerFactory, FakeLoggerFactory>();

        return builder;
    }
}
