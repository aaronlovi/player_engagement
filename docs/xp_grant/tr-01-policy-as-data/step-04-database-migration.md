# Step 4 — Database Migration

## Objective
Implement and validate schema changes that introduce policy storage while preserving existing claim functionality.

## Inputs
- Data model specification from Step 3.
- Migration tooling guidelines (Entity Framework migrations or equivalent) under `src/PlayerEngagement.Infrastructure`.
- `AGENTS.md` workflow checklist (build/test requirements).

## Tasks
- [ ] Scaffold migrations to create new tables, constraints, and default data where necessary.
- [ ] Ensure migrations are idempotent and safe to run in multiple environments.
- [ ] Write accompanying model snapshots or mapping updates required by the ORM layer.
- [ ] Execute migrations locally and confirm existing application startup plus `dotnet build` remain green.
- [ ] Capture migration testing notes (including potential data backfill scripts).

## Deliverables
- Checked-in migration files under `src/PlayerEngagement.Infrastructure/Persistence/Migrations`.
- Verification log summarizing local execution results and any seeding/backfill requirements.

## References
- `docs/xp_grant/xp_grant_technical_requirements.md` (TR-01, TR-06, TR-07).
- Internal DBA/DevOps guidelines if available.

## Open Questions
- Do we need a data backfill for historical policy references on existing claim records?
- What is the rollback procedure if migration fails mid-deployment?
