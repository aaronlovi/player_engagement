using System;
using System.Collections.Generic;
using System.Reflection;
using InnoAndLogic.Persistence;
using InnoAndLogic.Persistence.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Xp.Infrastructure.Persistence;

/// <summary>
/// Provides configuration methods for setting up database persistence services specific to the Player Engagement context.
/// </summary>
public static class XpDbmHostConfig {
    /// <summary>
    /// Configures services for the XpDbmService or XpDbmInMemoryService based on the database provider specified in <see cref="DatabaseOptions"/>.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The IConfiguration instance to bind options from.</param>
    /// <param name="sectionName">The name of the configuration section for DatabaseOptions.</param>
    /// <param name="externalMigrationAssemblies">
    /// An optional collection of assemblies containing additional embedded migration scripts.
    /// If not provided, only the default assembly is used.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/> with the configured services.</returns>
    public static IServiceCollection ConfigureXpPersistenceServices(
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
        return services.AddSingleton<IXpDbmService, XpDbmInMemoryService>(provider => {
            ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return new XpDbmInMemoryService(loggerFactory);
        });
    }

    private static IServiceCollection ConfigurePostgresServices(
        IServiceCollection services,
        IEnumerable<Assembly>? externalMigrationAssemblies) {
        Assembly[] migrationsAssemblies = externalMigrationAssemblies switch {
            null => Array.Empty<Assembly>(),
            IEnumerable<Assembly> assemblies => assemblies is Assembly[] array ? array : new List<Assembly>(assemblies).ToArray()
        };

        return services
            .AddSingleton<PostgresExecutor>()
            .AddSingleton(provider => CreateDbMigrations(provider, migrationsAssemblies))
            .AddSingleton<IXpDbmService>(CreatePostgresDbmService);
    }

    private static DbMigrations CreateDbMigrations(
        IServiceProvider provider,
        IReadOnlyCollection<Assembly> migrationAssemblies) {
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        IOptions<DatabaseOptions> databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>();

        return new DbMigrations(loggerFactory, databaseOptions, migrationAssemblies);
    }

    private static IXpDbmService CreatePostgresDbmService(IServiceProvider provider) {
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        PostgresExecutor executor = provider.GetRequiredService<PostgresExecutor>();
        DbMigrations migrations = provider.GetRequiredService<DbMigrations>();
        DatabaseOptions databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        return new XpDbmService(loggerFactory, executor, databaseOptions, migrations);
    }
}
