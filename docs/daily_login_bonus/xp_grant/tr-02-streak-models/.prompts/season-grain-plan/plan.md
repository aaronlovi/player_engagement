# SeasonGrain Plan for TR-02 Streak Models

## Scope & Assumptions

- Goal: Provide SeasonBoundaryInfo for Tiered Seasonal Reset. No existing SeasonGrain or season calendar schema; current “seasonal boosts” table is for XP multipliers, not season boundaries.
- Environment: single-instance Orleans app (local dev). Multi-node/keep-alive deferred. SeasonGrain must load required data from DbmService on activation and reject requests until loaded.
- Callers: future UserGrain or streak orchestration; no claim/eligibility flow exists yet. No milestone schema evolution or telemetry/metrics in this slice.

## Data Model & Db Path Needs

- Introduce a season calendar source separate from boost windows: fields `SeasonId`, `Label`, `StartDate` (DateOnly), `EndDate` (DateOnly), `CreatedAt`.
- Add DTO and DbmService read path (e.g., `GetCurrentSeasonAsync` returning current and next season if available). Use UTC dates for storage; convert to DateOnly in domain.
- Schema addition (future PR): `daily_login_bonus_xp_seasons` (season_id BIGINT, label TEXT, start_date DATE, end_date DATE, created_at TIMESTAMPTZ).

## Grain Contract & Behavior

- Interface: `SeasonBoundaryInfo GetCurrent();` (throws/not-ready until load completes), `Task RefreshAsync()` to reload from Dbm after admin changes or season end.
- Activation: On ActivateAsync, fetch current season from Dbm; if no current, store next season start/end to answer “not active yet” queries. Do not accept GetCurrent until load succeeds.
- State: cached SeasonBoundaryInfo? and optional NextSeasonBoundaryInfo? (for scheduling). Track LastLoadedAt for diagnostics.
- Requests: return current season boundary when active; if no season active, return null/next-start info; callers handle null.

## Refresh Strategy

- Triggers: manual `RefreshAsync` (invoked by admin change) and scheduled check at season end (single-instance timer). No prefetch beyond storing next season from the read response.
- If Dbm read fails, keep serving last successful season and log error; retry on next refresh trigger.

## Caller Integration

- Streak engine callers supply SeasonBoundaryInfo via StreakTransitionContext. Until claim/UserGrain orchestration exists, no wiring changes. When present, the caller will resolve SeasonGrain → SeasonBoundaryInfo before evaluating Tiered Seasonal Reset.

## Deferred Items

- Multi-node SeasonGrain lifecycle/placement; telemetry/metrics; grain unit test project; schema migration implementation; wiring into claim/UserGrain flow.

## Next Actions (when ready to implement)

- Add season DTO and DbmService query to fetch current/next season from new `daily_login_bonus_xp_seasons`.
- Implement SeasonGrain with load-on-activate, readiness gating, and refresh timer/endpoint.
- Update streak orchestration (future UserGrain/claim flow) to obtain SeasonBoundaryInfo from SeasonGrain before calling StreakEngine.
- Add docs/runbook for season admin changes and refresh flow.

## Metadata

### Status

success

### Confidence

Medium — plan aligns with single-instance scope; schema addition still speculative.

### Dependencies

- AGENTS.md
- TR-02 docs and current streak engine Seasonal handling
- DbmService extension for season calendar

### Open Questions

- Exact season admin workflow and how to trigger RefreshAsync; any need to expose next-season info to callers?

### Assumptions

- Seasons are global (or policy-wide) and stored in a new calendar table; only one active season at a time; single-instance app for now.
