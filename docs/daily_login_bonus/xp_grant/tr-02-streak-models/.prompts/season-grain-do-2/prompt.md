# Objective

- Continue SeasonGrain work by defining the season calendar data path and adding provider tests, per the prior plan (.prompts/season-grain-plan/plan.md) and do prompt.

## Context

- Stack: C#/.NET + Orleans; single-instance scope. Existing work: SeasonBoundaryProvider stub, SeasonCalendarDTO stub, DbmService stubs returning empty. No season schema or grain yet.
- Guardrails: AGENTS.md (no LINQ in prod, explicit usings, XML docs, no staging/committing without approval, UTC timestamps, no Dapper, pure functions). Only repo owner runs tests/build.
- Open decisions: API shape when no active season (null vs. include next-start info) deferred; SeasonGrain/claim wiring deferred.

## Requirements

- Define season calendar DTO shape and DbmService implementation for `GetCurrentSeasonAsync` (stub until schema exists; optionally outline migration).
- Add unit tests for SeasonBoundaryProvider using a fake Dbm service to verify load, ready gating, and caching/reset behavior.
- Keep scope to single-instance; do not add Orleans grain or schema migration yet; no staging/committing.
- Add XML docs for new public members; align namespaces/folders.
- Include a closing instruction to create the next do-prompt (`season-grain-do-3`) if scope remains.

## Plan

- Review .prompts/season-grain-plan/plan.md and current stubs to confirm gaps.
- Specify season DTO shape for current/next season; implement DbmService placeholder to reflect shape (no DB yet).
- Add SeasonBoundaryProvider tests with a fake Dbm service (load success, not-ready until load, refresh updates state, empty season returns null); mock Dbm since schema is absent.
- Update implementation notes with progress and note remaining items (grain, schema migration, wiring).
- If work remains, instruct to generate `.prompts/season-grain-do-3/prompt.md` for the next slice via `prompts/create-meta-prompt.md`, referencing the season-grain plan, and running via `prompts/run-prompt.md`.

## Outputs

- Code updates to SeasonBoundaryProvider/Dbm stubs/tests.
- docs/daily_login_bonus/xp_grant/tr-02-streak-models/.prompts/season-grain-do-2/implementation-notes.md with metadata.

## Verification

- Manual: review provider tests and Dbm stub shape; no tests/run locally per workflow; note `dotnet test src/PlayerEngagement.sln` for owner.

## Success Criteria

- Season data shape and Dbm method clarified; provider covered by unit tests; remaining scope documented; instruction given for next do prompt if needed.

## Practices

- Use rg for search; prefer apply_patch; avoid destructive git commands; default to ASCII; do not stage/commit without approval.
