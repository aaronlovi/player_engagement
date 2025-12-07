Objective
- Execute implementation work for TR-02 streak models by following the plan in docs/daily_login_bonus/xp_grant/tr-02-streak-models/.prompts/streak-models-plan/plan.md.

Context
- Stack: C#/.NET + Orleans + Postgres; Angular admin UI exists but scope is server-side streak engine and claim/eligibility wiring.
- Guardrails: AGENTS.md (no LINQ in prod, explicit usings, XML docs for new public members, UTC timestamps, no Dapper, prefer pure functions, Orleans grains as cache boundary). Workflow: only repo owner runs build/tests; do not run dotnet commands. Default to ASCII.
- References: streak-models-plan/plan.md, TR-02 high/low-level requirements, soft_decay_streak_model.md, milestone_options.md, XP grant HLD/technical/business requirements, implementation_plan.md steps 1–12.

Requirements
- Implement the plan steps (interfaces, model logic, tests, observability, policy validation, docs) in sequence or justified order; keep transitions deterministic/pure and policy-version aware.
- Preserve idempotency via award uniqueness; ensure eligibility uses same logic without writes.
- Keep model_state JSON schema explicit; serialize/deserialize safely.
- Add tests per model (unit/property) using xUnit without FluentAssertions; avoid running them locally per workflow, but note commands to run.
- Add XML docs for new public types/members; respect namespace/folder alignment.
- Avoid destructive git commands; do not commit.

Plan
- Read streak-models-plan/plan.md and source docs to restate tasks and dependencies.
- Implement engine contract/types and model_state schemas.
- Implement each model (Plateau/Cap, Weekly Cycle, Decay/Soft Reset, Tiered Seasonal Reset with SeasonGrain, Milestone Meta-Reward) with observability hooks and tests.
- Wire into claim/eligibility flows, policy/DTO validation, and model_state persistence.
- Add documentation/runbook updates for operator preview/messaging as needed.
- Record test commands for owner to run (e.g., dotnet test src/PlayerEngagement.sln).
- Update metadata in implementation-notes.

Outputs
- Code changes per plan (Domain/Infrastructure/Host as applicable).
- docs/daily_login_bonus/xp_grant/tr-02-streak-models/.prompts/streak-models-do/implementation-notes.md with summary and `## Metadata` (Status, Confidence, Dependencies, Open Questions, Assumptions).

Verification
- Manual: review changes for alignment with plan, requirements, observability, and idempotency; ensure tests are written; list `dotnet test src/PlayerEngagement.sln` for owner to run.

Success Criteria
- All planned features implemented with tests and observability; policy validation and model_state handling in place; documentation updated; implementation-notes.md populated with metadata.

Practices
- Use rg for search; prefer apply_patch for single-file edits; avoid destructive git commands; default to ASCII.
