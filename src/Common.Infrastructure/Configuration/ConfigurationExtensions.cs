using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Configuration
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services, string configurationFilePath)
        {
            return services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddConfiguration(configurationFilePath).Build());
        }

        public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, string configurationFilePath)
        {
            configurationBuilder.AddJsonFile(configurationFilePath);
            configurationBuilder.AddEnvironmentVariables();

            return configurationBuilder;
        }
    }
}
