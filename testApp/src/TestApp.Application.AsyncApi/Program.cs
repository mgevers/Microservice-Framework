using Common.Infrastructure;
using Common.Infrastructure.Auth;

namespace TestApp.Application.AsyncApi;

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
            .AddJwtAndAPIKeyAuthentication("", "");
    }

    private static void ConfigureApplication(WebApplication app)
    {
        app.AddCommonConfiguration();
        app.MapControllers();
    }
}
