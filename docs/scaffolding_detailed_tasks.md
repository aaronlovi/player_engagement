| ID | Section                 | Task Title                                  | Description / Acceptance Criteria                                                                                   | Input Dependencies         | Expected Output                                  | Complexity (1–3) | Notes / Tips                                                    |
| -- | ----------------------- | ------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- | -------------------------- | ------------------------------------------------ | ---------------- | --------------------------------------------------------------- |
| 1  | Environment Setup       | Initialize .NET Solution                    | Create a new .NET 8 solution named `XpService` with Orleans dependencies and `InnoAndLogic.Persistence` referenced. Solution lives under `src/` with host and supporting projects split (e.g., `XpService.Host`, `XpService.Domain`). Use the ASP.NET Core minimal API template as the starting point for the host so Orleans can share the same process as the HTTP surface. | None                       | `.sln` file and initial project structure.       | 1                | Run `dotnet new sln` in `src/` and `dotnet new web` for the host project; additional class libraries can be added as empty shells. |
| 2  | Environment Setup       | Add PostgreSQL Connection Config            | Add environment variables or appsettings for PostgreSQL connection (`Host`, `Port`, `DB`, `User`, `Password`).      | Task 1                     | Config section in `appsettings.Development.json` | 1                | Use the `Postgres` configuration section (`Postgres:Host`, etc.); env overrides follow `Postgres__Host` format. |
| 3  | Database Schema         | Create Migration Script V1__init_schema.sql | Write SQL script with tables `xp_users`, `xp_ledger`, `xp_balance`, `xp_streaks`, `xp_rules`, `xp_awards`.          | Technical requirements doc | `Sql/migrations/V1__init_schema.sql`             | 2                | Must conform to Evolve format and `xp` schema.                  |
| 4  | Database Schema         | Create Trigger for Balance Updates          | Write SQL script that updates `xp_balance` after inserting into `xp_ledger`.                                        | Task 3                     | `Sql/migrations/V2__balance_trigger.sql`         | 2                | Use `xp.fn_apply_ledger()` as specified.                        |
| 5  | Database Schema         | Create Seed Script for Default Rules        | Write SQL seed with example `daily_login`, `first_win_of_day` rules.                                                | Task 3                     | `Sql/seed/seed_rules.sql`                        | 1                | Use JSON fields for multipliers and streak policies.            |
| 6  | Backend Scaffolding     | Implement Evolve Runner                     | Implement Evolve runner class (`EvolveRunner.cs`) using `InnoAndLogic.Persistence`.                                 | Task 3, Task 4             | `Infra/EvolveRunner.cs`                          | 2                | Should run on startup if migrations not applied.                |
| 7  | Backend Scaffolding     | Setup Orleans Host                          | Add Orleans single-silo host in `Program.cs` with local clustering.                                                 | Task 1                     | Orleans configured in `Program.cs`.              | 2                | Use `Host.CreateDefaultBuilder()` + `UseOrleans()`.             |
| 8  | Backend Scaffolding     | Implement Npgsql Connection Factory         | Create `Db.cs` to manage NpgsqlDataSource and provide pooled connections.                                           | Task 2                     | `Data/Db.cs`                                     | 1                | Reuse helper patterns from `InnoAndLogic.Persistence`.          |
| 9  | Backend Scaffolding     | Create XpRepository                         | Implement repository with SQL for ledger, balance, streak, and rule operations.                                     | Task 8                     | `Data/XpRepository.cs`                           | 3                | Encapsulate SQL, use transactions for grants.                   |
| 10 | Orleans                 | Define Grain Interfaces                     | Define `IXpGrantGrain`, `IXpStreakGrain`, `IXpRuleGrain`.                                                           | Task 7                     | `Orleans/Abstractions/*.cs`                      | 2                | Use Orleans conventions and async Tasks.                        |
| 11 | Orleans                 | Implement XpRuleGrain                       | Implements caching and CRUD for XP rules using repository.                                                          | Task 9, Task 10            | `Orleans/Grains/XpRuleGrain.cs`                  | 2                | Cache short TTL (e.g., 60s).                                    |
| 12 | Orleans                 | Implement XpStreakGrain                     | Handles streak updates based on policy JSON.                                                                        | Task 9, Task 10            | `Orleans/Grains/XpStreakGrain.cs`                | 3                | Implement both calendar-day and rolling-24h types.              |
| 13 | Orleans                 | Implement XpGrantGrain                      | Applies grants atomically using repository + streak/rule grains.                                                    | Task 11, Task 12           | `Orleans/Grains/XpGrantGrain.cs`                 | 3                | Enforce idempotency via ledger unique key.                      |
| 14 | API Layer               | Setup Minimal API Endpoints                 | Configure `Program.cs` or `XpEndpoints.cs` with routes `/xp/grant`, `/xp/balance/{user_id}`, `/xp/rules`.           | Task 7, Task 9             | `Api/XpEndpoints.cs`                             | 2                | Use `MapPost` / `MapGet`.                                       |
| 15 | API Layer               | Implement DTOs                              | Create request/response DTOs for grant, balance, and rule endpoints.                                                | Task 14                    | `Api/Dtos.cs`                                    | 1                | Follow JSON naming consistency.                                 |
| 16 | Angular UI              | Initialize Angular Project                  | Create new Angular app `xp-config-ui`.                                                                              | None                       | `/ui/xp-config-ui` directory                     | 1                | Use Angular CLI.                                                |
| 17 | Angular UI              | Implement Rules List Component              | Fetch `/xp/rules` and display in table.                                                                             | Task 14                    | `rules-list.component.ts`                        | 2                | Include columns for key, base XP, caps, multipliers.            |
| 18 | Angular UI              | Implement Rule Editor Component             | Form editor for adding/updating XP rules.                                                                           | Task 14                    | `rule-editor.component.ts`                       | 2                | Use reactive forms; submit via PUT `/xp/rules/{rule_key}`.      |
| 19 | Angular UI              | Implement API Service                       | Create service for backend interaction (`xp-api.service.ts`).                                                       | Task 14                    | `xp-api.service.ts`                              | 1                | Handle CRUD + simulate endpoint.                                |
| 20 | Testing                 | Implement xUnit Setup                       | Add base test project and PostgreSQL Testcontainers.                                                                | Task 1                     | `XpService.Tests` project.                       | 2                | Run migrations during test setup.                               |
| 21 | Testing                 | Write Grant Tests                           | Validate grant logic, caps, and idempotency.                                                                        | Task 13, Task 20           | `GrantTests.cs`                                  | 3                | Use seeded rules + mock users.                                  |
| 22 | Testing                 | Write Streak Tests                          | Validate streak increment logic (calendar vs rolling).                                                              | Task 12, Task 20           | `StreakTests.cs`                                 | 2                | Verify grace day policy.                                        |
| 23 | Testing                 | Write Rule Tests                            | Verify rule CRUD and JSON parsing correctness.                                                                      | Task 11, Task 20           | `RulesTests.cs`                                  | 1                | Include boundary test cases.                                    |
| 24 | Configuration & Logging | Implement Logging                           | Add structured console logging for grants and migrations.                                                           | Task 7                     | Integrated logs in `Program.cs`                  | 1                | Use `ILogger` and `.AddConsole()`.                              |
| 25 | Configuration & Logging | Setup CORS                                  | Allow Angular origin for `/xp/*` endpoints.                                                                         | Task 14, Task 16           | Config entry in `Program.cs`.                    | 1                | Add `WithOrigins("http://localhost:4200")`.                     |
| 26 | Finalization            | Verify Migrations & Seeds                   | Ensure Evolve applies migrations and seeds automatically.                                                           | Task 6, Task 5             | Verified schema in PostgreSQL.                   | 1                | Check table presence manually.                                  |
| 27 | Finalization            | Validate End-to-End Flow                    | Simulate a daily login grant via API and verify balance/streaks.                                                    | Task 13–Task 15            | Test results or logs.                            | 2                | Confirms full XP pipeline working.                              |

## Preferred Project Structure

> Updated to mirror your **Identity** repo conventions (Host/Gateway/Grains/Common/Protos, with infra-level migrations).

| Folder / File           | Purpose                                                            |
| ----------------------- | ------------------------------------------------------------------ |
| `/src/Xp.sln`           | Solution file (groups all Xp projects).                            |
| `/src/Xp.Host/`         | Orleans silo host (startup, Orleans config, health).               |
| `/src/Xp.Gateway/`      | Public HTTP API (Minimal API/Controllers) for XP endpoints.        |
| `/src/Xp.Grains/`       | Orleans grains and domain logic (Grant, Streak, Rule).             |
| `/src/Xp.Common/`       | Shared code (DTOs, repository, Evolve runner, Npgsql data source). |
| `/src/Xp.Protos/`       | (Optional) gRPC contracts if needed later.                         |
| `/src/Xp.Tests/`        | xUnit tests (unit + integration; can spin ephemeral Postgres).     |
| `/infra/migrations/xp/` | Evolve SQL migration scripts (`V1__...sql`, `V2__...sql`).         |
| `/infra/seeds/xp/`      | SQL seed files (e.g., `seed_rules.sql`).                           |
| `/docs/`                | Design notes/ADRs related to XP (optional).                        |
| `/ui/xp-config-ui/`     | Angular app for XP rules configuration.                            |

## Pathing for Tasks

| Task Area                           | Where artifacts should live                                                                                                                               |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Migrations & Seeds**              | `/infra/migrations/xp`, `/infra/seeds/xp`                                                                                                                 |
| **Orleans Grains**                  | `/src/Xp.Grains`                                                                                                                                          |
| **Grain Interfaces (Abstractions)** | `/src/Xp.Grains/Abstractions` or directly under `Xp.Grains`                                                                                               |
| **Npgsql Repository & Evolve**      | `/src/Xp.Common/Data` and `/src/Xp.Common/Infra`                                                                                                          |
| **Mappers**                         | `/src/Xp.Common/Mappers` — similar to `Identity.Common/Mappers`; include classes like `XpRuleMapper`, `XpGrantMapper` for DTO↔Domain conversions.         |
| **API Endpoints & DTOs**            | `/src/Xp.Gateway/Api`                                                                                                                                     |
| **Host Startup**                    | `/src/Xp.Host`                                                                                                                                            |
| **Tests**                           | `/src/Xp.Tests` (integration tests; spin Postgres, run migrations from `/infra/migrations/xp`)                                                            |
| **Common Unit Tests**               | `/src/Xp.Common.Tests` — xUnit tests for all components in `/src/Xp.Common`, including repositories, data utilities, mappers, and infrastructure helpers. |
| **Grains Unit Tests**               | `/src/Xp.Grains.Tests` — xUnit tests for grain behavior (e.g., streak logic, grant application).                                                          |

### Mapper Pattern (from Identity.Common/Mappers)

**Intent**: Keep controllers/grains free of translation concerns by centralizing DTO↔Domain conversions in small, *pure* mapping helpers.

**Style**:

* A dedicated static class per aggregate (e.g., `UserMapper`, `XpRuleMapper`, `XpGrantMapper`).
* Public methods named `ToDomain(...)` and `ToDto(...)` with 1:1 field mapping.
* No side‑effects, I/O, DI, or business rules; mapping only.
* Defensive null handling and safe defaults (empty collections instead of null; trim strings where appropriate).
* Consistent normalization rules (e.g., timestamps → UTC; IDs preserved as provided; enums mapped by name with strict validation).

**Typical Layout** (mirroring `UserMapper.cs` conventions):

* `ToDomain(Dto dto)`: constructs a domain model from API/transport DTO, performing minimal normalization (e.g., `.Trim()`, casing where relevant) and validating required fields.
* `ToDto(Domain model)`: constructs a DTO for API responses or persistence boundaries, flattening nested objects only when necessary.
* Small helper methods for repeated conversions (e.g., enum/string, DateTimeOffset/Instant).

**Usage in XP project**:

* `XpRuleMapper` — maps between `XpRuleDto` ⇄ `XpRule` (JSON policy blobs remain opaque `JsonDocument`/`string` as appropriate).
* `XpGrantMapper` — maps request DTOs to domain `GrantCommand` and domain results to response DTOs.
* `XpLedgerMapper` — maps DB result sets → internal ledger models → response DTOs.

**Testing Guidance**:

* Unit test each mapper in `/src/Xp.Common.Tests` with round‑trip tests (`ToDomain` then `ToDto` reproduces inputs where expected), null/empty edge cases, and enum error cases.
* Avoid mocking; treat as pure functions with table‑driven tests.

**Why this pattern?**

* Keeps grains and endpoints focused on orchestration & rules, not field plumbing.
* Enables strict, testable boundaries and safer refactors when DTOs or domain models evolve.
