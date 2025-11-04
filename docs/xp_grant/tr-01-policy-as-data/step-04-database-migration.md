# Step 4 — Database Migration

## Objective
Implement and validate schema changes that introduce policy storage while preserving existing claim functionality.

## Inputs
- Data model specification from Step 3.
- Migration tooling guidelines (Entity Framework migrations or equivalent) under `src/PlayerEngagement.Infrastructure`.
- `AGENTS.md` workflow checklist (build/test requirements).

## Tasks
- [x] Scaffold migrations to create new tables, constraints, and default data where necessary.
- [x] Ensure migrations are idempotent and safe to run in multiple environments.
- [x] Write accompanying model snapshots or mapping updates required by the ORM layer.
- [x] Execute migrations locally and confirm existing application startup plus `dotnet build` remain green.
- [x] Capture migration testing notes (including potential data backfill scripts).

## Summary
- Added `src/PlayerEngagement.Infrastructure/Persistence/Migrations/V002__policy_as_data.sql` to create `xp_policies`, `xp_policy_versions`, `xp_policy_streak_curve`, `xp_policy_seasonal_boosts`, and `xp_policy_segment_overrides`, including constraints, comments, and partial unique index enforcing a single published version per policy.
- Seasonal boosts enforce non-overlapping windows via a GiST exclusion constraint (with `btree_gist` extension guarded by `create extension if not exists`).
- Legacy `xp_rules` table is dropped because the implementation is still greenfield; no data migration required.
- `dotnet build src/PlayerEngagement.sln` runs clean (see build output captured after generating the migration).

## Deliverables
- Migration script: `src/PlayerEngagement.Infrastructure/Persistence/Migrations/V002__policy_as_data.sql`
- Build verification record confirming the infrastructure project compiles after migrations are added.

## References
- `docs/xp_grant/tr-01-policy-as-data/data_model.md`
- `AGENTS.md` workflow checklist (build requirement satisfied).

## Open Questions
- Determine initial seed data (if any) before rollout; currently no default policies are inserted.
- Decide when to add foreign keys from `xp_awards` to the new policy version table (optional enhancement).

## Deliverables
- Checked-in migration files under `src/PlayerEngagement.Infrastructure/Persistence/Migrations`.
- Verification log summarizing local execution results and any seeding/backfill requirements.

## References
- `docs/xp_grant/xp_grant_technical_requirements.md` (TR-01, TR-06, TR-07).
- Internal DBA/DevOps guidelines if available.

## Open Questions
- Do we need a data backfill for historical policy references on existing claim records?
- What is the rollback procedure if migration fails mid-deployment?
