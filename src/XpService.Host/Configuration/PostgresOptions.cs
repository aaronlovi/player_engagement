namespace XpService.Host.Configuration;

public sealed class PostgresOptions
{
    public const string SectionName = "Postgres";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5432;

    public string Database { get; init; } = "xp_service";

    public string Username { get; init; } = "xp_service";

    public string Password { get; init; } = "postgres";

    public string Schema { get; init; } = "xp";
}
