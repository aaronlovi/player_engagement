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


## Run the Angular Workspace

Navigate to the UI workspace and start the dev server (after installing dependencies once).
```bash
cd ui/player-engagement-config-ui
npm install
npm run start
```
- The app serves on `http://localhost:4200`.
- To proxy API calls, launch with `npm run start -- --proxy-config proxy.conf.json` after creating the proxy file described in the workspace README.

## Stop Services

Press `Ctrl+C` to stop the host, then tear down infrastructure if desired:
```bash
docker compose -f infra/docker-compose.yml down
```

## Angular Placeholder Page

- Once the host is running, start the UI (`npm run start` inside `ui/player-engagement-config-ui`).
- The home page fetches `/health/live`, `/health/ready`, `/xp/ledger`, and `/xp/grants` to verify the scaffold.
- Expect the XP responses to return `501` with stub messaging until real workflows arrive.

- Placeholder page shows live/ready health and XP stubs with 501 responses.

## Verification Checklist

1. `docker compose -f infra/docker-compose.yml up -d`
2. `dotnet run --project src/PlayerEngagement.Host` (leave running)
3. In a new terminal: `cd ui/player-engagement-config-ui && npm run start`
4. Visit `http://localhost:4200` and confirm the placeholder page shows:
   - `/health/live` status + JSON payload
   - `/health/ready` status + JSON payload
   - `/xp/ledger` returning a 501 stub
   - `/xp/grants` returning a 501 stub
5. Stop the Angular server (`Ctrl+C`) and tear down compose when finished.
