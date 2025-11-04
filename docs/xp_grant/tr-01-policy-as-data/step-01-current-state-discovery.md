# Step 1 — Current-State Discovery

## Objective
Establish how the daily login XP grant is currently implemented so we can identify gaps between the live system and TR-01 requirements.

## Inputs
- `AGENTS.md` workflow checklist and development conventions.
- Existing XP grant implementation under `src/PlayerEngagement.*` (especially Infrastructure and Host projects).
- Documentation: `docs/xp_grant/xp_grant_high_level_design.md`, `docs/xp_grant/xp_grant_technical_requirements.md`, and `docs/xp_grant/xp_grant_business_requirements.md`.

## Tasks
- [ ] Audit current policy-related code paths: claim orchestration, configuration sources, streak logic, and any hard-coded reward values.
- [ ] Identify data sources (config files, environment variables, feature flags) that influence XP amounts or streaks.
- [ ] Trace how claim requests flow from API entry points through persistence to the XP ledger.
- [ ] Document discrepancies against TR-01 acceptance criteria (versioned policies, immutable history, policy references on claims).
- [ ] Capture dependency notes (e.g., existing caching layers, ORM usage) that will inform downstream design tasks.

## Deliverables
- Short discovery report (Markdown or ticket comment) summarizing existing behavior, notable constraints, and missing capabilities.
- Inventory of files/modules that will require modification when introducing policy-as-data.

## References
- `docs/xp_grant/xp_grant_glossary.md` for shared terminology.
- `docs/daily_login_bonus.md` for motivation context.

## Open Questions
- Are there legacy policy experiments or feature flags that need migration into the new model?
- Which teams own the current configuration touchpoints (engineering vs. live-ops)?
