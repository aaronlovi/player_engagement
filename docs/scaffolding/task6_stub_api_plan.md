# Task 6 – Stub Minimal API Surface Plan

Goal: expose `/health/live`, `/health/ready`, and placeholder `/xp/*` endpoints inside the existing Orleans host without introducing business logic.

**Status:** Completed — Minimal API stub endpoints added to `src/PlayerEngagement.Host/Program.cs` and build verified with `dotnet build`.

## Step-by-Step Checklist

- [x] Confirm context  
  Re-read Task 6 description in `docs/scaffolding/scaffolding_detailed_tasks.md` and reviewed `src/PlayerEngagement.Host/Program.cs`.
- [x] Decide layout  
  Keep endpoint registration inline in `Program.cs` for the scaffolding phase.
- [x] Add health endpoints  
  Mapped `GET /health/live` and `GET /health/ready` with readiness wired to `IPlayerEngagementDbmService`.
- [x] Add XP placeholders  
  Created `/xp` route group with `GET /xp/ledger` and `POST /xp/grants` returning 501 JSON stubs.
- [x] Wire services into endpoints  
  Injected `IPlayerEngagementDbmService` via minimal API signatures where required.
- [x] Enable endpoint metadata  
  Added `.WithName(...)` and `.WithTags(...)` metadata to health and XP routes.
- [x] Manual verification  
  Built the solution locally via `dotnet build src/PlayerEngagement.sln` after restoring packages.
- [ ] Update documentation  
  Add references to the new endpoints in `docs/scaffolding/quickstart.md` (handled in Task 8).
