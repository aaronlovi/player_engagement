# Repository Guidelines

## Project Structure & Module Organization

All solution code lives under `src`. The `PlayerEngagement.sln` ties together `PlayerEngagement.Domain` for business rules, `PlayerEngagement.Infrastructure` for persistence and Orleans wiring, and `PlayerEngagement.Host` for the ASP.NET/Orleans host. SQL migrations live in `src/PlayerEngagement.Infrastructure/Persistence/Migrations`. Product and design context stays in `docs/` (see the engagement mechanic references). Workflow and coding guidelines live in this `AGENTS.md` file so there is a single source of truth.

The front-end/admin interface sits in `ui/player-engagement-config-ui`, built with Angular. Run `ng serve` from that directory for local development (optionally with `--proxy-config proxy.conf.json` to forward `/xp/*` calls to the host) to manage engagement configuration through the browser.

Current implementation work targets a local development environment. Use Docker Desktop plus the compose files under `infra/` for background services such as Postgres or Redis; cloud/Kubernetes deployment is out of scope for now.

When modeling back-end behavior, favor Orleans grains as the primary caching boundary. Each grain should reload its state (including policy configuration) from persistence on activation and avoid relying on distributed caches like Redis unless a specific cross-grain composition requires it. This keeps cache coherence simple and resilient if a node goes offline.

For database design, avoid using the Dapper library; rely on hand-written data access aligned with project conventions. Tables should typically include an identity `BIGINT` primary key unless a compelling reason dictates a different strategy. Default to non-nullable columns—store empty strings instead of null for text fields, and only allow nullable timestamps when semantically required (e.g., `obsoleted_at`).

Avoid LINQ in production C# code paths; favor explicit loops and conditionals for clarity and performance. LINQ is acceptable in unit tests where brevity outweighs allocation costs.
Persist timestamps as UTC `DateTime` values; avoid `DateTimeOffset` in the code unless a specific offset is required.
Provide an `Invalid` member with explicit value 0 for enum types unless there is a strong reason not to.

Keep C# namespaces aligned with folder structure (e.g., file `Policies/Mappers/Foo.cs` uses namespace `PlayerEngagement.Infrastructure.Policies.Mappers`).

## Build, Test, and Development Commands

- `dotnet restore src/PlayerEngagement.sln` – hydrate external packages.
- `dotnet build src/PlayerEngagement.sln` – compile all projects with warnings treated as actionable.
- `dotnet run --project src/PlayerEngagement.Host` – launch the Orleans silo and health endpoints on localhost.
- `dotnet test src/PlayerEngagement.sln` – execute the solution test suite (add `--collect:"XPlat Code Coverage"` when validating coverage).
- `docker compose -f infra/docker-compose.yml up -d` – start Postgres/pgAdmin dependencies.

## Workflow Checklist

**Critical collaboration flow (must follow for every task):**

1. I do the coding work.
2. You (repo owner) perform the final code review.
3. You run the compile step.
4. You run the unit tests.
5. You report the compile/test results back to me.
6. If everything passes, I create the commit.

- Do not stage or commit any changes unless the repo owner explicitly asks you to or explicitly approves staging/committing for that task.

- Only the repo owner runs compile/tests to avoid polluting this assistant’s context; I will not execute those commands.
- Never update the implementation plan, stage changes, or create a commit until you have completed your review and shared compile/test results per steps 2–5 above.

**Supporting guardrails:**

- Keep tasks atomic—scope changes to the current task and resolve blockers immediately.
- Favor application-layer logic—keep domain rules in C# services; use DB triggers/functions only with a documented need.
- Document deviations—when a task cannot meet these standards, record the reason and next steps in the relevant planning doc.

Refer back to this checklist before finalizing any task.

## Coding Style & Naming Conventions (Backend / .NET)

Adopt default .NET formatting: four-space indentation, file-scoped namespaces when practical, PascalCase for classes and public members, camelCase for locals, and suffix asynchronous methods with `Async`. Keep `internal` types inside their domain project and prefer small, focused files. Run `dotnet format` before submitting changes to ensure consistent spacing, ordering, and usings.

- Keep functions short and cohesive; break out repeated or complex logic into helpers to stay DRY and leave `Program.Main` as a thin orchestration layer.
- Prefer injecting `ILoggerFactory` and creating typed loggers from it; avoid injecting concrete `ILogger<T>` directly.
- Place each type in its own file; enums normally live in an `Enums.cs` file within the appropriate namespace.
- Provide XML documentation comments for every type (classes, records, enums) and each member or enum value when introducing new code.
- Do not enable implicit usings or rely on global `using` directives in C# projects; include explicit usings at the top of each file so dependencies remain obvious and localized.
- Before adding a private utility/helper, decide if it should live in `PlayerEngagement.Shared` (and be generalized for reuse) instead of staying local—opt for shared, well-scoped helpers when they benefit multiple projects.
- When handling numeric input from the admin UI, bind integer fields to `int/long` and multipliers/percentages to `decimal`; reject non-integers for integral fields in validation. If numeric strings must be accepted, enable `JsonNumberHandling.AllowReadingFromString` on the relevant DTOs and still enforce the same range checks.
- Avoid magic numbers—introduce named constants (e.g., `const int WeeklyCycleLength = 7`) for repeated or meaningful values and reference them instead of inline literals.
- Do not use nested classes/records; keep all types top-level in their own files for clarity.
- Avoid LINQ in production code paths to reduce allocations; LINQ is acceptable only in unit tests where brevity matters.
- Provide XML documentation for all public types and members (classes, records, methods, properties, fields) to keep contracts explicit.

## Frontend (Angular) Guidelines

- **Project structure:** Use a feature/core layout. Place shared services/utilities under `src/app/core` (e.g., `core/api`, `core/utils`, `core/models/types`) and feature components under `src/app/features/<area>/...`.
- **Routing:** Keep routes in `app.routes.ts` and feature-specific routing (when needed) colocated with the feature.
- **Services/HTTP:** Centralize API services in `core/api`. Prefer typed request/response DTOs and a shared HTTP helper for result/state mapping. Inject base URLs/tokens via Angular DI/environment.
- **Forms/validation:** Use reactive forms for admin flows; group controls with explicit types and validators. Surface API validation errors visibly to the user (toasts/inline).
- **Form UX:** Provide clear, descriptive tooltips (`title` attributes or help icons) on form fields so operators can quickly recall purpose, constraints, and side effects without leaving the page.
- **Templates:** Keep component templates thin—minimal logic or JS in HTML. Push computations, lookups, and branching into the component class and expose simple bindings for the view.
- **Template smell check:** If a template binding needs more than a simple property (e.g., multiple dotted accesses or inline `find`/`map`/`filter`), move that logic into the component class and bind to a helper instead.
- **Error handling:** Wrap API calls in try/catch and surface status/error in view state; avoid unhandled promise rejections.
- **Styling:** Keep global shell styles minimal; scope feature styles with component stylesheets. Prefer consistent button/link styles and badges across features.
- **Testing:** Add unit tests for services and key components; e2e for critical flows (create→publish, retire, overrides) when feasible.
- **Auth/config:** Read API base URLs and auth headers from environment files; avoid hardcoding secrets.

## Testing Guidelines

Target xUnit for new tests, mirroring project structure (e.g., `src/PlayerEngagement.Domain.Tests`). Name test classes `<TypeUnderTest>Tests` and methods `Method_Scenario_ExpectedOutcome`. Guard external dependencies with fakes or use the compose stack for integration coverage. Run `dotnet test` locally and watch for flaky behavior by rerunning critical suites with `--filter`.

- **Assertion libraries:** Do not use `FluentAssertions` in C# test projects; stick with xUnit’s built-in `Assert` APIs (or MSTest/NUnit equivalents if ever introduced) to keep diagnostics consistent across suites.
- **Database unit tests:** Keep unit tests focused on business logic. Skip direct tests of Postgres statements (anything under `src/PlayerEngagement.Infrastructure/Persistence/Statements`). Exercise persistence behaviors through `IPlayerEngagementDbmService` and especially `PlayerEngagementDbmInMemoryService`, which provides a safe test double. Reserve the actual Postgres-backed `PlayerEngagementDbmService` for future integration tests that run against a live database.
- **Test factories:** In C# tests and frontend/Angular tests, build DTOs/records/view models through test factory helpers that accept `null` or defaults for every parameter and coalesce to sensible fallback values. Avoid direct `new` calls with long parameter lists inside tests.
- **Guard patterns:** Prefer `Math.Max/Math.Min` (or type-appropriate equivalents) over ternaries for simple clamping to keep intent clear.

## Commit & Pull Request Guidelines

Follow the existing history: imperative, capitalized subjects (`Add`, `Refactor`, `Update`) limited to ~72 characters. Squash incidental work before pushing. Each PR should describe the change, outline testing performed, link relevant design docs or issues, and include screenshots or API traces when behavior changes. Update `docs/` or configuration notes alongside code so reviewers can trace the impact.

- Do not create commits or push changes until the repository owner reviews the diff; share staged work for approval first.

## Security & Configuration Tips

Never commit secrets; keep `.env` files local and document required variables in PRs. Align `appsettings.Development.json` with the compose values, and prefer environment variables for production secrets. When introducing new services, extend `infra/docker-compose.yml` and document port usage to avoid collisions.
