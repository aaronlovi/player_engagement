# Objective

- Implement the remaining SeasonGrain tasks: add the season schema/migration and wire SeasonGrain/provider with real Dbm data, per `.prompts/season-grain-plan/plan.md`.

## Context

- Stack: C#/.NET + Orleans; single-instance scope. Current state: SeasonBoundaryProvider with current+next support, Dbm stubs and statement to fetch seasons, in-memory season storage; migration not added; SeasonGrain not implemented.
- Guardrails: AGENTS.md (no LINQ in prod, explicit usings, XML docs, no staging/committing without approval, UTC timestamps, no Dapper, pure functions). Only repo owner runs tests/build.
- Open decisions: how to surface “no active season” (null vs. next-start info) — currently returning null.

## Requirements

- Add migration to create `daily_login_bonus_xp_seasons` table (season_id BIGINT PK, label TEXT, start_date DATE, end_date DATE, created_at TIMESTAMPTZ).
- Implement Dbm query path end-to-end (DTO mapping, statement already present) and ensure in-memory DBM mirrors the schema.
- Implement SeasonGrain (single instance) using Dbm provider: load on activation, block until ready, return SeasonBoundaryInfo (null when none), support Refresh after admin changes/season end.
- Add XML docs/tests as feasible (season statement/provider/grain); keep scope single-instance; no metrics yet.
- If any scope remains, then create `season-grain-do-5` via `prompts/create-meta-prompt.md` referencing the plan.

## Plan

- Add migration SQL for `daily_login_bonus_xp_seasons`; wire into migrations list.
- Ensure Dbm `GetCurrentSeasonAsync` uses the new table; update in-memory DBM data setters/getters as needed.
- Implement SeasonGrain contract/activation/readiness using SeasonBoundaryProvider/Dbm; return null when no active season.
- Add/update tests where possible; update implementation notes; if scope remains, create season-grain-do-5.

## Outputs

- Migration SQL, Dbm wiring, SeasonGrain implementation, tests if added; docs in `.prompts/season-grain-do-4/implementation-notes.md` with metadata.

## Verification

- Manual: review migration, Dbm/grain wiring; no tests/build run; note `dotnet test src/PlayerEngagement.sln` for owner.

## Success Criteria

- Season table created/mapped; SeasonGrain loads from Dbm with readiness gating; remaining scope documented and next prompt generated if needed.

## Practices

- Use rg for search; prefer apply_patch; avoid destructive git commands; default to ASCII; do not stage/commit without approval.
