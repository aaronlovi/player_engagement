# Objective

- Continue SeasonGrain work by defining the actual season schema/Db path and outlining SeasonGrain wiring, per .prompts/season-grain-plan/plan.md and prior do prompts.

## Context

- Stack: C#/.NET + Orleans; single-instance scope. Current state: SeasonBoundaryProvider and season DTO/Dbm stubs (return empty), provider tests using fake Dbm. No season schema or Orleans grain yet.
- Guardrails: AGENTS.md (no LINQ in prod, explicit usings, XML docs, no staging/committing without approval, UTC timestamps, no Dapper, pure functions). Only repo owner runs tests/build.
- Open decisions: API shape for “no active season” (null vs. next-start info), integration into claim/UserGrain flows deferred.

## Requirements

- Define season calendar schema/DTO and implement `GetCurrentSeasonAsync` with a real Dbm query (or detailed stub + migration outline).
- Outline SeasonGrain contract/activation/readiness (load before serving; single instance) and how it will supply SeasonBoundaryInfo.
- Keep scope to single-instance; no multi-node resilience or metrics yet.
- Add XML docs for new types/members; align namespaces/folders.
- If work remains, include instruction to generate the next do-prompt (`season-grain-do-4`) via `prompts/create-meta-prompt.md` referencing the plan, and run via `prompts/run-prompt.md`.

## Plan

- Review .prompts/season-grain-plan/plan.md and current provider stubs/tests.
- Specify season schema (table fields) and SeasonCalendarDTO shape; implement DbmService method (or stub with clear migration notes).
- Sketch SeasonGrain contract/activation/readiness behavior (even if not coded yet); note wiring steps.
- Update implementation notes with outcomes and deferrals; if scope remains, instruct creation of season-grain-do-4.

## Outputs

- Code/docs updates for season schema/Dbm method (stub or implementation), SeasonGrain outline, and updated implementation notes under `.prompts/season-grain-do-3/implementation-notes.md`.

## Verification

- Manual: review schema/Dbm changes and SeasonGrain outline; no tests/build run; note `dotnet test src/PlayerEngagement.sln` for owner when applicable.

## Success Criteria

- Season schema/Dbm path defined or stubbed with migration plan; SeasonGrain contract/activation outlined; remaining scope documented with next prompt instructions if needed.

## Practices

- Use rg for search; prefer apply_patch; avoid destructive git commands; default to ASCII; do not stage/commit without approval.
