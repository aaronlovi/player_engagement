using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.Serialization;
using PlayerEngagement.Infrastructure.Persistence;

namespace PlayerEngagement.Host;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Host.UseOrleans((context, siloBuilder) =>
        {
            _ = siloBuilder.UseLocalhostClustering()
                .ConfigureLogging(logging => logging.AddConsole())
                .ConfigureServices(services =>
                {
                    _ = services.AddSerializer(serializerBuilder =>
                    {
                        _ = serializerBuilder.AddProtobufSerializer(
                            isSerializable: type => type.Namespace?.StartsWith("Identity.Protos") == true,
                            isCopyable: type => type.Namespace?.StartsWith("Identity.Protos") == true);
                    });
                });
        });

        builder.Logging.ClearProviders();

        _ = builder.Services.AddHttpLogging(logging => {
            logging.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode;
        });

        const string CorsPolicyName = "LocalDevCors";
        _ = builder.Services.AddCors(options => {
            options.AddPolicy(name: CorsPolicyName, configurePolicy: policy => {
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        Assembly[] migrationAssemblies = [
            typeof(PlayerEngagementDbmService).Assembly
        ];
        _ = builder.Services.ConfigurePlayerEngagementPersistenceServices(
            builder.Configuration,
            "DbmOptions",
            migrationAssemblies);

        WebApplication app = builder.Build();

        await EnsureDatabaseAsync(app.Services);

        app.UseRequestPipelineLogging();
        app.UseCors(CorsPolicyName);

        MapHealthEndpoints(app);
        MapXpEndpoints(app);

        await app.RunAsync();
    }

    private static async Task EnsureDatabaseAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        IPlayerEngagementDbmService dbmService = scope.ServiceProvider.GetRequiredService<IPlayerEngagementDbmService>();
        Console.WriteLine("DbmService resolved and migrations tested.");
        await dbmService.HealthCheckAsync(CancellationToken.None);
    }

    private static void MapHealthEndpoints(WebApplication app)
    {
        _ = app.MapGet("/health/live", () =>
                Results.Ok(new { status = "live" }))
            .WithName("HealthLive")
            .WithTags("Health");

        _ = app.MapGet("/health/ready", async (IPlayerEngagementDbmService dbmService, CancellationToken ct) =>
        {
            try
            {
                await dbmService.HealthCheckAsync(ct);
                return Results.Ok(new { status = "ready" });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new { status = "unavailable", error = ex.Message },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
            .WithName("HealthReady")
            .WithTags("Health");
    }

    private static void MapXpEndpoints(WebApplication app)
    {
        RouteGroupBuilder xpGroup = app.MapGroup("/xp").WithTags("XP");

        // TODO: Replace scaffolding stubs with real handlers once XP workflows are implemented.
        _ = xpGroup.MapGet("/ledger", () =>
                Results.Json(
                    new { message = "Not implemented - scaffolding stub" },
                    statusCode: StatusCodes.Status501NotImplemented))
            .WithName("XpGetLedger");

        _ = xpGroup.MapPost("/grants", () =>
                Results.Json(
                    new { message = "Not implemented - scaffolding stub" },
                    statusCode: StatusCodes.Status501NotImplemented))
            .WithName("XpPostGrants");
    }
}
