using System;
using System.Collections.Generic;
using System.Reflection;
using InnoAndLogic.Persistence;
using InnoAndLogic.Persistence.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PlayerEngagement.Infrastructure.Persistence;

/// <summary>
/// Provides configuration methods for setting up database persistence services specific to the Player Engagement context.
/// </summary>
public static class PlayerEngagementDbmHostConfig {
    /// <summary>
    /// Configures services for the PlayerEngagementDbmService or PlayerEngagementDbmInMemoryService based on the database provider specified in <see cref="DatabaseOptions"/>.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The IConfiguration instance to bind options from.</param>
    /// <param name="sectionName">The name of the configuration section for DatabaseOptions.</param>
    /// <param name="externalMigrationAssemblies">
    /// An optional collection of assemblies containing additional embedded migration scripts.
    /// If not provided, only the default assembly is used.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/> with the configured services.</returns>
    public static IServiceCollection ConfigurePlayerEngagementPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        IEnumerable<Assembly>? externalMigrationAssemblies = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(sectionName))
            throw new ArgumentException("Configuration section name cannot be null or whitespace.", nameof(sectionName));

        IConfigurationSection optionsSection = configuration.GetSection(sectionName);
        _ = services.AddOptions<DatabaseOptions>()
            .Configure(optionsSection.Bind);

        DatabaseOptions databaseOptions = optionsSection.Get<DatabaseOptions>() ?? new DatabaseOptions();

        return databaseOptions.Provider switch {
            DatabaseProvider.InMemory => ConfigureInMemoryServices(services),
            DatabaseProvider.Postgres => ConfigurePostgresServices(services, externalMigrationAssemblies),
            _ => throw new InvalidOperationException($"Unsupported database provider: {databaseOptions.Provider}")
        };
    }

    private static IServiceCollection ConfigureInMemoryServices(IServiceCollection services) {
        return services.AddSingleton<IPlayerEngagementDbmService, PlayerEngagementDbmInMemoryService>(provider => {
            ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return new PlayerEngagementDbmInMemoryService(loggerFactory);
        });
    }

    private static IServiceCollection ConfigurePostgresServices(
        IServiceCollection services,
        IEnumerable<Assembly>? externalMigrationAssemblies) {
        IReadOnlyCollection<Assembly> migrationsAssemblies = BuildMigrationAssemblies(externalMigrationAssemblies);

        return services
            .AddSingleton<PostgresExecutor>()
            .AddSingleton(provider => CreateDbMigrations(provider, migrationsAssemblies))
            .AddSingleton<IPlayerEngagementDbmService>(CreatePostgresDbmService);
    }

    private static IReadOnlyCollection<Assembly> BuildMigrationAssemblies(IEnumerable<Assembly>? externalAssemblies) {
        var uniqueAssemblies = new HashSet<Assembly>();
        var orderedAssemblies = new List<Assembly>();

        AddIfMissing(typeof(PlayerEngagementDbmHostConfig).Assembly);

        if (externalAssemblies != null) {
            foreach (Assembly assembly in externalAssemblies) {
                if (assembly is not null)
                    AddIfMissing(assembly);
            }
        }

        return orderedAssemblies;

        void AddIfMissing(Assembly assembly) {
            if (uniqueAssemblies.Add(assembly))
                orderedAssemblies.Add(assembly);
        }
    }

    private static DbMigrations CreateDbMigrations(
        IServiceProvider provider,
        IReadOnlyCollection<Assembly> migrationAssemblies) {
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        IOptions<DatabaseOptions> databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>();

        return new DbMigrations(loggerFactory, databaseOptions, migrationAssemblies);
    }

    private static IPlayerEngagementDbmService CreatePostgresDbmService(IServiceProvider provider) {
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        PostgresExecutor executor = provider.GetRequiredService<PostgresExecutor>();
        DbMigrations migrations = provider.GetRequiredService<DbMigrations>();
        DatabaseOptions databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        return new PlayerEngagementDbmService(loggerFactory, executor, databaseOptions, migrations);
    }
}
