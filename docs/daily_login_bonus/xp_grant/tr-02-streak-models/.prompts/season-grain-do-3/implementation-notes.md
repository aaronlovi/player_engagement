# SeasonGrain Do-3 Implementation Notes

## Work done

- Added season calendar DTO with optional next season (`SeasonCalendarWithNextDTO`) and wired `GetCurrentSeasonAsync` through Dbm service (real statement stub) and in-memory Dbm.
- Added SQL statement stub `GetCurrentSeasonStmt` to fetch current/next season from `daily_login_bonus_xp_seasons` (migration pending).
- SeasonBoundaryProvider now caches next season; still returns null when no active season; SeasonGrain not yet implemented.

## Next steps / follow-on prompt

- Add migration for `daily_login_bonus_xp_seasons` and tests for Dbm query/provider with real data.
- Implement SeasonGrain using the Dbm-backed provider; decide whether to surface next-start info when no active season.
- If scope remains, generate `.prompts/season-grain-do-4/prompt.md` via `prompts/create-meta-prompt.md`, referencing `.prompts/season-grain-plan/plan.md`, and run it with `prompts/run-prompt.md`.

## Metadata

### Status

partial

### Confidence

Medium — Dbm path and provider adjustments added; migration and grain still pending.

### Dependencies

- AGENTS.md
- .prompts/season-grain-plan/plan.md
- SeasonBoundaryProvider/Dbm stubs

### Open Questions

- Migration details and admin workflow; handling of “no active season”/next-start in API responses.

### Assumptions

- Single-instance scope; Dbm/season data to be defined before implementation.
