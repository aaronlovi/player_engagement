# Objective

- Plan SeasonGrain design/implementation to supply SeasonBoundaryInfo for Tiered Seasonal Reset, aligned with TR-02 context and current repo state.

## Context

- Stack: C#/.NET + Orleans; current code has `SeasonBoundaryInfo` and streak engine expects it, but there is no SeasonGrain or season calendar schema. Existing seasonal data is policy seasonal boosts (`daily_login_bonus_xp_policy_seasonal_boosts`), which are boost windows, not authoritative season boundaries.
- Guardrails: AGENTS.md (no LINQ in prod, explicit usings, XML docs, no staging/committing without approval, UTC timestamps, no Dapper, pure functions). Only repo owner runs tests/build.
- Scope: single-instance SeasonGrain (local dev), load-on-activate from DbmService; reject requests until loaded; multi-node/keep-alive out of scope. No evolution of milestone schema or telemetry now.
- Open questions/decisions from user: season data source not defined; SeasonGrain should load required data before serving; UserGrain will call SeasonGrain in future; no prefetch of next season unless needed; refresh triggers likely admin changes or season end; if no current season, grain should know when next season starts to activate.

## Requirements

- Define data source needs (schema/DTO) for season calendar (season id, start/end dates) separate from boost windows.
- Design SeasonGrain contract (methods, state, activation behavior) and how it populates SeasonBoundaryInfo; specify blocking/ready behavior until load completes.
- Identify integration points: how streak engine callers obtain SeasonBoundaryInfo; how refresh is triggered (admin API/season end).
- Keep plan aligned with current single-instance scope; defer multi-node concerns.
- Note verification steps (unit tests for grain if/when a test project is added) and what is deferred.

## Plan

- Restate scope/assumptions and current gaps (no season schema, only boost data).
- Propose season data model/DTO and DbmService read path needed to support SeasonGrain.
- Define SeasonGrain interface/state/activation flow (load via Dbm, block until ready; expose current season; handle no-current-season + next-start).
- Outline refresh triggers (admin change, season end) and how to schedule/track.
- Describe how callers (future UserGrain/streak orchestration) obtain SeasonBoundaryInfo.
- Call out deferred items (multi-node resiliency, telemetry, tests until grain test harness exists).

## Outputs

- docs/daily_login_bonus/xp_grant/tr-02-streak-models/.prompts/season-grain-plan/plan.md with plan and ## Metadata (Status, Confidence, Dependencies, Open Questions, Assumptions).

## Verification

- Manual: ensure plan.md covers data model, grain contract, activation/loading behavior, refresh triggers, caller integration, and notes deferrals.

## Success Criteria

- Clear, actionable plan for SeasonGrain and supporting data path, scoped to current single-instance dev environment, with deferrals documented.

## Practices

- Use rg for search; prefer apply_patch for edits; avoid destructive git commands; default to ASCII; do not run tests/build. Do not stage/commit without explicit approval.
