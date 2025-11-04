# Repository Guidelines

## Project Structure & Module Organization
All solution code lives under `src`. The `PlayerEngagement.sln` ties together `PlayerEngagement.Domain` for business rules, `PlayerEngagement.Infrastructure` for persistence and Orleans wiring, and `PlayerEngagement.Host` for the ASP.NET/Orleans host. SQL migrations live in `src/PlayerEngagement.Infrastructure/Persistence/Migrations`. Product and design context stays in `docs/` (see `docs/workflow_guidelines.md` and the engagement mechanic references). Local infrastructure manifests are under `infra/`, where `docker-compose.yml` provisions Postgres and pgAdmin.

## Build, Test, and Development Commands
- `dotnet restore src/PlayerEngagement.sln` – hydrate external packages.
- `dotnet build src/PlayerEngagement.sln` – compile all projects with warnings treated as actionable.
- `dotnet run --project src/PlayerEngagement.Host` – launch the Orleans silo and health endpoints on localhost.
- `dotnet test src/PlayerEngagement.sln` – execute the solution test suite (add `--collect:"XPlat Code Coverage"` when validating coverage).
- `docker compose -f infra/docker-compose.yml up -d` – start Postgres/pgAdmin dependencies.

## Coding Style & Naming Conventions
Adopt default .NET formatting: four-space indentation, file-scoped namespaces when practical, PascalCase for classes and public members, camelCase for locals, and suffix asynchronous methods with `Async`. Keep `internal` types inside their domain project and prefer small, focused files. Run `dotnet format` before submitting changes to ensure consistent spacing, ordering, and usings.
- Keep functions short and cohesive; break out repeated or complex logic into helpers to stay DRY and leave `Program.Main` as a thin orchestration layer.

## Testing Guidelines
Target xUnit for new tests, mirroring project structure (e.g., `src/PlayerEngagement.Domain.Tests`). Name test classes `<TypeUnderTest>Tests` and methods `Method_Scenario_ExpectedOutcome`. Guard external dependencies with fakes or use the compose stack for integration coverage. Run `dotnet test` locally and watch for flaky behavior by rerunning critical suites with `--filter`.

## Commit & Pull Request Guidelines
Follow the existing history: imperative, capitalized subjects (`Add`, `Refactor`, `Update`) limited to ~72 characters. Squash incidental work before pushing. Each PR should describe the change, outline testing performed, link relevant design docs or issues, and include screenshots or API traces when behavior changes. Update `docs/` or configuration notes alongside code so reviewers can trace the impact.
- Do not create commits or push changes until the repository owner reviews the diff; share staged work for approval first.

## Security & Configuration Tips
Never commit secrets; keep `.env` files local and document required variables in PRs. Align `appsettings.Development.json` with the compose values, and prefer environment variables for production secrets. When introducing new services, extend `infra/docker-compose.yml` and document port usage to avoid collisions.
