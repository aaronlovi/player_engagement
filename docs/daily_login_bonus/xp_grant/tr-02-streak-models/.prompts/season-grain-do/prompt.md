# Objective

- Implement the SeasonGrain plan to supply SeasonBoundaryInfo for Tiered Seasonal Reset, per .prompts/season-grain-plan/plan.md.

## Context

- Stack: C#/.NET + Orleans; single-instance scope (local dev). No SeasonGrain or season calendar schema exists yet. Current season data is only policy seasonal boosts (not usable for boundaries).
- Guardrails: AGENTS.md (no LINQ in prod, explicit usings, XML docs, no staging/committing without approval, UTC timestamps, no Dapper, pure functions). Only repo owner runs tests/build.
- Plan reference: .prompts/season-grain-plan/plan.md (new season schema/DTO, SeasonGrain load-on-activate via DbmService, readiness gating, single-instance).
- Open decision: API shape when no active season (null vs. explicit next-start info).

## Requirements

- Add season calendar DTO and DbmService read path for current/next season (schema TBD: season_id, label, start/end dates).
- Define SeasonGrain contract and implementation: activate → load season via Dbm; reject requests until loaded; expose current SeasonBoundaryInfo (null when none); refresh mechanism (manual method, timer at season end).
- Keep scope to single-instance; no multi-node/keep-alive, no metrics/telemetry yet.
- Add XML docs for new public types/members; align namespaces/folders.
- Note any deferred pieces (schema migration, metrics, wiring callers).
- Do not stage/commit without explicit approval.

## Plan

- Review .prompts/season-grain-plan/plan.md and existing DTOs to confirm gaps.
- Add season calendar DTO and DbmService interface method stubs (no schema migration).
- Implement SeasonGrain interface/activation/readiness logic (load before serving; manual refresh; optional next-start info TBD).
- Add minimal tests if feasible (or note deferral if no grain test harness).
- Update implementation notes with outcomes and deferrals.

## Outputs

- Code changes under src for SeasonGrain/DTOs/DbmService stubs (no schema migration).
- docs/daily_login_bonus/xp_grant/tr-02-streak-models/.prompts/season-grain-do/implementation-notes.md with summary and ## Metadata (Status, Confidence, Dependencies, Open Questions, Assumptions).

## Verification

- Manual: review grain contract/activation logic, DTOs, and DbmService stub; ensure no tests/build run; note commands for owner (`dotnet test src/PlayerEngagement.sln`) once applicable.

## Success Criteria

- SeasonGrain contract and supporting DTO/Dbm stubs exist with readiness gating and refresh hooks; deferrals (schema migration, metrics, caller wiring) documented.

## Practices

- Use rg for search; prefer apply_patch; avoid destructive git commands; default to ASCII; do not stage/commit without approval.
