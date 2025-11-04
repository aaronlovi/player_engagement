using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Serialization;
using PlayerEngagement.Infrastructure.Persistence;
using Serilog;

namespace PlayerEngagement.Host;

internal static class Program {
    private static async Task Main(string[] args) {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try {
            Log.Information("Starting the application...");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            _ = builder.Host.UseOrleans((context, siloBuilder) => {
                _ = siloBuilder.UseLocalhostClustering() // Use localhost clustering for local development
                    .ConfigureLogging(logging => logging.AddConsole())
                    .UseDashboard(_ => { })
                    .ConfigureServices(services => {
                        _ = services.AddSerializer(_ => {
                            // Keep the following for the future when Protobuf serialization is needed.
                            // _ = serializerBuilder.AddProtobufSerializer(
                            //     isSerializable: type => type.Namespace?.StartsWith("Identity.Protos") == true,
                            //     isCopyable: type => type.Namespace?.StartsWith("Identity.Protos") == true);
                        });
                    });
            });

            _ = builder.Services.AddHttpLogging(logging => {
                logging.LoggingFields = HttpLoggingFields.RequestMethod
                    | HttpLoggingFields.RequestPath
                    | HttpLoggingFields.ResponseStatusCode;
            });

            const string CorsPolicyName = "LocalDevCors";
            _ = builder.Services.AddCors(options => {
                options.AddPolicy(name: CorsPolicyName, configurePolicy: policy => {
                    _ = policy.WithOrigins("http://localhost:4200")
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

            _ = app.UseRequestPipelineLogging();
            _ = app.UseCors(CorsPolicyName);

            MapHealthEndpoints(app);
            MapXpEndpoints(app);

            Log.Information("Starting web host");

            await app.RunAsync();
        } catch (Exception ex) {
            Log.Fatal(ex, "Host terminated unexpectedly");
        } finally {
            Log.CloseAndFlush();
        }
    }

    private static async Task EnsureDatabaseAsync(IServiceProvider services) {
        using IServiceScope scope = services.CreateScope();
        IPlayerEngagementDbmService dbmService = scope.ServiceProvider.GetRequiredService<IPlayerEngagementDbmService>();
        Log.Information("DbmService resolved and migrations tested.");
        try {
            Result res = await dbmService.HealthCheckAsync(CancellationToken.None);
            Log.Information("Database health check completed successfully.");
        } catch (Exception ex) {
            Log.Error(ex, "Database health check failed");
            throw;
        }
    }

    private static void MapHealthEndpoints(WebApplication app) {
        _ = app.MapGet("/health/live", () =>
                Results.Ok(new { status = "live" }))
            .WithName("HealthLive")
            .WithTags("Health");

        _ = app.MapGet("/health/ready", async (IPlayerEngagementDbmService dbmService, CancellationToken ct) => {
            try {
                _ = await dbmService.HealthCheckAsync(ct);
                return Results.Ok(new { status = "ready" });
            } catch (Exception ex) {
                return Results.Json(
                    new { status = "unavailable", error = ex.Message },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("HealthReady")
        .WithTags("Health");
    }

    private static void MapXpEndpoints(WebApplication app) {
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
