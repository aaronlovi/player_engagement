# Step 1 — Current-State Discovery

## Objective
Establish how the daily login XP grant is currently implemented so we can identify gaps between the live system and TR-01 requirements.

## Inputs
- `AGENTS.md` workflow checklist and development conventions.
- Existing XP grant implementation under `src/PlayerEngagement.*` (especially Infrastructure and Host projects).
- Documentation: `docs/xp_grant/xp_grant_high_level_design.md`, `docs/xp_grant/xp_grant_technical_requirements.md`, and `docs/xp_grant/xp_grant_business_requirements.md`.

## Tasks
- [x] Audit current policy-related code paths: claim orchestration, configuration sources, streak logic, and any hard-coded reward values.
- [x] Identify data sources (config files, environment variables, feature flags) that influence XP amounts or streaks.
- [x] Trace how claim requests flow from API entry points through persistence to the XP ledger.
- [x] Document discrepancies against TR-01 acceptance criteria (versioned policies, immutable history, policy references on claims).
- [x] Capture dependency notes (e.g., existing caching layers, ORM usage) that will inform downstream design tasks.

## Findings
- No policy logic has been implemented beyond the initial persistence shell. The only references to XP policy data are the `xp_rules` and `xp_awards` tables defined in `src/PlayerEngagement.Infrastructure/Persistence/Migrations/V001__init_schema.sql:125`.
- `src/PlayerEngagement.Host/Program.cs:103` exposes `/xp` endpoints, but both are scaffold placeholders returning `501 Not Implemented`; there is no claim orchestration, streak logic, or policy retrieval code.
- `src/PlayerEngagement.Infrastructure/ServiceCollectionExtensions.cs:16` and `Persistence/PlayerEngagementDbmHostConfig.cs:27` wire up database services via the shared InnoAndLogic persistence abstractions. These services currently provide only migration execution and health checks.
- Configuration input for persistence lives in `src/PlayerEngagement.Host/appsettings.Development.json:7`, specifying Postgres connection details. There are no environment variables, feature flags, or JSON configuration entries governing policy behavior.
- Because no runtime components consume `xp_rules`, claims cannot reference `policy_version`, and TR-01 acceptance criteria (CRUD endpoints, immutable history usage, policy references per claim) are unmet.

## Impacted Components Inventory
- Database schema: `src/PlayerEngagement.Infrastructure/Persistence/Migrations/V001__init_schema.sql`
- Infrastructure setup: `src/PlayerEngagement.Infrastructure/ServiceCollectionExtensions.cs`, `src/PlayerEngagement.Infrastructure/Persistence/PlayerEngagementDbmHostConfig.cs`, `src/PlayerEngagement.Infrastructure/Persistence/PlayerEngagementDbmService.cs`
- Host scaffolding: `src/PlayerEngagement.Host/Program.cs`
- Configuration: `src/PlayerEngagement.Host/appsettings*.json`

## Gaps vs. TR-01
- Policy documents exist only as a database table definition; there is no code to create, version, or retrieve them.
- Claim processing is absent, so claims cannot reference policy versions or append XP ledger entries per requirements TR-04 and TR-07.
- There is no admin UI support for policy authoring; the Angular app currently contains only CLI scaffolding.
- Observability and audit trails for policy changes are non-existent.

## Deliverables
- Short discovery report (Markdown or ticket comment) summarizing existing behavior, notable constraints, and missing capabilities.
- Inventory of files/modules that will require modification when introducing policy-as-data.

## References
- `docs/xp_grant/xp_grant_glossary.md` for shared terminology.
- `docs/daily_login_bonus.md` for motivation context.

## Open Questions
- Legacy policy experiments or feature flags to migrate? → None; this is a greenfield implementation with no prior experiments.
- Team ownership of configuration touchpoints? → Currently a single developer maintains the system; future contributors will be onboarded later.
