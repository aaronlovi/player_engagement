# Step 3 — Data Modeling

## Objective
Translate the domain model into a persistence design that supports versioned, immutable policies with segment mappings and audit history.

## Inputs
- Approved domain model from Step 2.
- Technical requirements (`docs/xp_grant/xp_grant_technical_requirements.md`) with emphasis on TR-01, TR-06, TR-07, TR-09, TR-10.
- High-level design data contracts (`docs/xp_grant/xp_grant_high_level_design.md`).
- Existing database schema under `src/PlayerEngagement.Infrastructure/Persistence/Migrations`.

## Tasks
- [ ] Choose storage approach (normalized tables vs. JSON documents) compatible with current ORM/persistence patterns.
- [ ] Design tables/entities for policies, policy versions, segment-policy mappings, and audit metadata.
- [ ] Define indexes and constraints (unique policy version identifiers, foreign keys to segments).
- [ ] Plan migration ordering and rollback strategy (idempotent migrations, data seeding approach).
- [ ] Update ERD or schema documentation to include new artifacts.

## Deliverables
- Data model specification (diagram or structured table definitions) reviewed with persistence owners.
- Draft migration plan with sequencing and back-out strategy.

## References
- `AGENTS.md` repository and migration conventions.
- Any existing ERD documentation within `docs/` or infrastructure notes.

## Open Questions
- Should policy payloads be stored as JSON for flexibility or decomposed into relational columns?
- Do we require soft-delete flags or GPA (governance, policy, audit) retention policies that affect schema design?
