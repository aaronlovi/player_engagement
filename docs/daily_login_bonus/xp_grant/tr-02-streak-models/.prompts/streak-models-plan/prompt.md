Objective
- Plan the implementation work for TR-02 streak models by turning docs/daily_login_bonus/xp_grant/tr-02-streak-models/implementation_plan.md into an actionable engineering plan that aligns with repo guidelines.

Context
- Stack: C#/.NET, Orleans, Postgres; Angular admin UI exists but focus here is server-side streak engine work.
- Guardrails: see AGENTS.md (no LINQ in prod paths, explicit usings, XML docs for new public members, pure streak transitions, deterministic/stateful per policy version). CI/tests are run by the repo owner; do not run dotnet commands.
- Source materials: TR-02 high/low-level requirements, soft_decay_streak_model.md, milestone_options.md, overall XP grant technical/business/HLD docs, implementation_plan.md (steps 1–12).
- No existing slugged prompts for this topic.

Requirements
- Produce a written implementation plan covering all steps in implementation_plan.md, incorporating high/low-level TR-02 requirements, grace/season/milestone rules, and observability/testing expectations.
- Keep the plan deterministic/pure-function focused and policy-version aware; note model-state schema expectations and idempotency.
- Include where outputs should live (plan.md) with metadata block per create-meta-prompt guidelines.
- Respect AGENTS.md coding/testing constraints (no LINQ in prod, explicit usings, XML docs for new public types, avoid Dapper, UTC timestamps).

Plan
- Re-read implementation_plan.md and TR-02 requirement docs to restate scope.
- Identify dependencies and inputs/outputs for the streak engine contract.
- Outline tasks per model (Plateau/Cap, Weekly Cycle Reset, Decay/Soft Reset, Tiered Seasonal Reset, Milestone Meta-Reward) including grace/miss handling and observability.
- Cover integration hooks (eligibility/claim flows, policy/DTO validation, model_state schema) and testing/metrics/logging expectations.
- Specify deliverables, owners (if relevant), and sequencing consistent with steps 1–12; call out open questions or risks.

Outputs
- docs/daily_login_bonus/xp_grant/tr-02-streak-models/.prompts/streak-models-plan/plan.md (with ## Metadata block: Status, Confidence, Dependencies, Open Questions, Assumptions).

Verification
- Manual: ensure plan.md exists with all sections above, references to TR-02 docs, and metadata block filled; confirm paths align with implementation_plan.md.

Success Criteria
- Plan.md provides step-by-step actions aligned to all 12 implementation_plan steps, maps requirements to tasks, notes dependencies/risks, and specifies expected outputs/tests/observability for each model.

Practices
- Use rg for search; prefer apply_patch for single-file edits; avoid destructive git commands; default to ASCII.
