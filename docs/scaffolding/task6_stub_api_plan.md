# Task 6 – Stub Minimal API Surface Plan

Goal: expose `/health/live`, `/health/ready`, and placeholder `/xp/*` endpoints inside the existing Orleans host without introducing business logic.

## Step-by-Step Checklist

1. **Confirm context**
   - Re-read `docs/scaffolding/scaffolding_detailed_tasks.md` Task 6 description to ensure scope stays limited to stubs.
   - Open `src/PlayerEngagement.Host/Program.cs` to inspect current host setup.

2. **Decide layout**
   - Determine whether to keep endpoint registration inline in `Program.cs` or extract to a helper (e.g., `Api/EndpointRegistration.cs`). For the scaffold, inline is acceptable if it stays short.

3. **Add health endpoints**
   - Use the Minimal API builder to map `GET /health/live` returning `Results.Ok(new { status = "live" })`.
   - Map `GET /health/ready` to call `IPlayerEngagementDbmService.HealthCheckAsync` and return 200/503 based on the result.

4. **Add XP placeholders**
   - Create route group `/xp` to host placeholder endpoints such as:
     - `GET /xp/ledger` → `Results.StatusCode(StatusCodes.Status501NotImplemented)`.
     - `POST /xp/grants` → same 501 response (include TODO comment referencing future tasks).
   - Ensure responses include minimal JSON describing the stub (e.g., `{ message = "Not implemented – scaffolding stub" }`).

5. **Wire services into endpoints**
   - Register `IPlayerEngagementDbmService` with DI (already configured) and inject where needed via `app.MapGet(..., async (IPlayerEngagementDbmService dbm, CancellationToken ct) => ...)`.

6. **Enable endpoint metadata**
   - Add `.WithName(...)` and `.WithTags("Health")` or `"XP"` to aid future documentation.

7. **Manual verification**
   - Run the host (`dotnet run --project src/PlayerEngagement.Host`) after ensuring Postgres container is up (if required for the readiness check).
   - Hit the new endpoints via `curl` or browser to confirm expected status codes/body.

8. **Update documentation**
   - Note endpoint URLs and behavior in `docs/scaffolding/quickstart.md` (to be created/updated by Task 8).
   - Mark Task 6 as in progress/completed in `scaffolding_detailed_tasks.md` once implementation is merged.
