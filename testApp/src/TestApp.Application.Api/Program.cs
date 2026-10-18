using Common.Infrastructure;
using Common.Infrastructure.Auth;
using TestApp.Core.CommandHandlers;

namespace TestApp.Application.Api;

public partial class Program
{
    public static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddCommonConfiguration();
        ConfigureServices(builder.Services);

        var app = builder.Build();
        ConfigureApplication(app);

        return app.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services
            .AddJwtAndAPIKeyAuthentication("", "")
            .AddMediatR(config => config.RegisterServicesFromAssemblies(typeof(AddCharacterRequestHandler).Assembly));
    }

    private static void ConfigureApplication(WebApplication app)
    {
        app.AddCommonConfiguration();
        app.MapControllers();
    }
}