# TR-02 — Streak Models (High-Level Requirements)

## Objective

Support configurable daily login streak models for the XP Grant so policies can choose how streaks grow, reset, and award XP without code changes.

## Primary Requirements

- Support the five models: Plateau/Cap, Weekly Cycle Reset, Decay/Soft Reset (round down/clamp to min 1), Tiered Seasonal Reset, and Milestone Meta-Reward (XP-only/flag until non-XP assets exist).
- Streak computation is deterministic and pure-function: given policy, prior streak state, reward-day id, and claim timing, the next state and XP modifiers are predictable.
- Policy selection drives behavior: every claim references a policy version whose streak model dictates streak transitions and XP curve resolution.
- Idempotent claims: multiple attempts in the same reward day return the same streak/XP result; streak state mutates only once per reward day.
- Timezone-safe: streak evaluation uses reward-day boundaries derived from anchor timezone/claim window (TR-03) to avoid DST or tz exploits.
- Observability: emit logs/metrics tracing streak model decisions (model type, day index, grace/decay usage, season boundary hits, milestone unlocks).
- Milestone handling options and the current stance are documented in `docs/daily_login_bonus/xp_grant/milestone_options.md`.
- Grace is applied before model-specific mechanics, capped by policy, and does not cross season boundaries.
- Preview/eligibility responses should include upcoming milestone/season notes (e.g., “milestone in 2 days”, “season resets in 3 days”).

## Outcomes & Acceptance

- Each model has documented behavior, parameters, and constraints that map to policy definitions.
- Unit/property tests cover streak transitions per model (including misses, grace, season boundaries, and milestone unlocks).
- Claim/eligibility flows return consistent streak counters and XP modifiers for all models under concurrent requests.
- Operators can preview outcomes (eligibility/dry-run) for any model; results match claim logic.
