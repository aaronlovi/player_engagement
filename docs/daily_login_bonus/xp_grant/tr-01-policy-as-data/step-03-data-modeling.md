# Step 3 — Data Modeling

## Objective

Translate the domain model into a persistence design that supports versioned, immutable policies with segment mappings and audit history.

## Inputs

- Approved domain model from Step 2.
- Technical requirements (`docs/xp_grant/xp_grant_technical_requirements.md`) with emphasis on TR-01, TR-06, TR-07, TR-09, TR-10.
- High-level design data contracts (`docs/xp_grant/xp_grant_high_level_design.md`).
- Existing database schema under `src/PlayerEngagement.Infrastructure/Persistence/Migrations`.

## Tasks

- [x] Choose storage approach (normalized tables vs. JSON documents) compatible with current ORM/persistence patterns.
- [x] Design tables/entities for policies, policy versions, segment-policy mappings, and audit metadata.
- [x] Define indexes and constraints (unique policy version identifiers, foreign keys to segments).
- [x] Plan migration ordering and rollback strategy (idempotent migrations, data seeding approach).
- [x] Update ERD or schema documentation to include new artifacts.

## Summary

- `docs/xp_grant/tr-01-policy-as-data/data_model.md` captures the relational schema: `xp_policies`, `xp_policy_versions`, `xp_policy_streak_curve`, `xp_policy_seasonal_boosts`, and `xp_policy_segment_overrides`, preserving `xp_awards` for claim history. The old `xp_rules` table will be replaced during migrations.
- Core policy attributes are modeled as first-class columns to support querying and constraints. JSONB is limited to `streak_model_parameters` and optional `seasonal_metadata`, which do not influence SQL filtering.
- Constraints enforce single published version per policy, positive XP amounts, contiguous streak day indexes (validated in domain layer), and non-overlapping seasonal boosts (via exclusion constraint).
- Migration sequencing is defined: add new tables, backfill (if needed), keep `xp_awards` untouched, and drop/rename `xp_rules` once unused. All changes target the Docker-provisioned Postgres database.
- Soft delete / retention policies are deferred; archival semantics rely on immutable version history.

## Deliverables

- Data model specification: `docs/xp_grant/tr-01-policy-as-data/data_model.md`

## References

- `AGENTS.md` repository and migration conventions.
- Any existing ERD documentation within `docs/` or infrastructure notes.

## Open Questions

- Should policy payloads be stored as JSON for flexibility or decomposed into relational columns? → Resolved: primary fields use relational columns; JSONB retained only for infrequently queried model parameters.
- Do we require soft-delete flags or GPA retention policies? → Deferred; immutable version history provides necessary audit trail for initial release.

## Deliverables

- Data model specification (diagram or structured table definitions) reviewed with persistence owners.
- Draft migration plan with sequencing and back-out strategy.

## References

- `AGENTS.md` repository and migration conventions.
- Any existing ERD documentation within `docs/` or infrastructure notes.

## Open Questions

- Should policy payloads be stored as JSON for flexibility or decomposed into relational columns?
- Do we require soft-delete flags or GPA (governance, policy, audit) retention policies that affect schema design?
