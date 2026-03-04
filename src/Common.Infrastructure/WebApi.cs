using Common.Infrastructure.OpenTelemetry;
using Common.Infrastructure.OpenTelemetry.Enrichers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using System.Reflection;

namespace Common.Infrastructure;

public static class WebApi
{
    public static WebApplicationBuilder AddCommonConfiguration(this WebApplicationBuilder builder, string? serviceName = null)
    {
        builder.UseSerilog(serviceName ?? Assembly.GetExecutingAssembly().FullName!);

        builder.Services
            .AddLogging()
            .AddCors()
            .AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

        builder.Services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen();

        return builder;
    }

    public static void AddCommonConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(builder =>
                builder
                    .SetIsOriginAllowed(origin => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        }

        app.UseMiddleware<RequestLogContext>();
        app.UseSerilogRequestLogging();

        app
            .UseSwagger()
            .UseSwaggerUI();
    }
}
