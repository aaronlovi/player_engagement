# Scaffolding Quickstart

Follow these steps to bring up the Player Engagement scaffold locally and exercise the stub endpoints.

## Prerequisites
- .NET 8 SDK installed.
- Docker engine available for the Postgres stack.
- NuGet packages restored (`dotnet restore src/PlayerEngagement.sln`).

## Start Local Infrastructure
```bash
docker compose -f infra/docker-compose.yml up -d
# Optional: check readiness
 docker exec player-engagement-db pg_isready -U postgres -d player_engagement_db_local
```

## Run the Host
```bash
dotnet run --project src/PlayerEngagement.Host
```
- The console should log "Database health check completed successfully" followed by Orleans silo startup messages.
- Request logging is enabled via `UseHttpLogging`; each request prints method, path, and status.

## Smoke-Test Endpoints
With the host running in one terminal, use a second shell:
```bash
# Live health check
curl -s http://localhost:5000/health/live

# CORS preflight using Angular dev origin
curl -i -X OPTIONS \
  -H "Origin: http://localhost:4200" \
  -H "Access-Control-Request-Method: GET" \
  http://localhost:5000/health/live
```
- Expect `{"status":"live"}` for the first call.
- The preflight should return `204` with `Access-Control-Allow-Origin` and `Access-Control-Allow-Methods` headers, confirming the CORS policy.

## Stop Services
Press `Ctrl+C` to stop the host, then tear down infrastructure if desired:
```bash
docker compose -f infra/docker-compose.yml down
```
