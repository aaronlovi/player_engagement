# TR-02 — Streak Models (Low-Level Requirements)

## Contracts & Inputs

- Inputs: policy version (model type + parameters + streak curve), prior streak state (`current_streak`, `longest_streak`, `grace_used`, `last_reward_day_id`, model_state), reward-day id, and claim timestamp.
- Outputs: new streak state, XP multiplier/additive bonus (from streak curve and model logic), receipt fields returned to clients, and any milestone/season events emitted.
- Determinism: transition function must be pure with respect to inputs; no reliance on wall-clock beyond reward-day resolution.

## Data & Persistence

- Streak state persists per user in `daily_login_bonus_xp_streaks` (TR-06) and is recomputable from awards; model-specific state is stored in `model_state` JSON.
- Awards reference `policy_key`/`policy_version` to guarantee the correct model was applied at claim time.
- Policy definitions hold `streak_model_type`, `streak_model_parameters`, and the streak curve rows; validation rejects unknown model types or malformed parameters.

## Model Behaviors (by type)

- Plateau/Cap: increment streak until `PlateauDay`, then clamp; apply `PlateauMultiplier` at and after plateau. Missed day resets to 0 unless grace window covers it.
- Weekly Cycle Reset: streak counts 1..7 and resets to 1 on the 8th reward day after a claimed sequence; grace only prevents breakage within the grace window.
- Decay/Soft Reset: after `GraceDay`, apply `DecayPercent`, round **down** (floor) to an integer, then clamp to at least 1: `next = max(1, floor(current_streak * (1 - decayPercent)))`. Use this effective streak day for the streak curve.
- Tiered Seasonal Reset: streak accumulates within season; reset to day 1 on the first claim after the hard season end. Season boundaries come from an Orleans SeasonGrain (authoritative start/end) referenced by season_id; policy seasonal metadata is a fallback. The grain reads season data via the Dbm service on activation and re-reads at season end to pick up the next season. No carryover into the next season. Tiers define day ranges and multipliers/bonuses; tiers must not overlap.
- Milestone Meta-Reward: streak growth follows the curve; when `current_streak` reaches configured milestones, apply normal XP and mark the milestone in award/state for idempotency. Defer external reward events until non-XP assets exist.

## Grace & Miss Handling

- Miss detection is based on gap between `last_reward_day_id` and current reward day. If gap ≤ configured grace window and remaining `grace_allowed_misses` permits, apply grace **before** any model-specific mechanics (e.g., decay/reset) and keep the streak intact; otherwise apply the model rules.
- Grace consumption is capped at the policy level (total `grace_allowed_misses`); consume one per missed reward day covered. When grace covers a miss, the current claim advances the streak by 1 (subject to curve caps).
- Grace cannot bridge season boundaries in Tiered Seasonal Reset; streak resets on the first claim after the hard season end.
- Track `grace_used` and surface in analytics; apply grace before model-specific decay/reset logic.

## XP Calculation

- Streak curve day index is derived from effective streak day after applying model rules (including decay floor+clamp). The curve can cap growth; model multipliers/bonuses compose with curve values without exceeding policy-defined limits. Milestone hits are recorded in award metadata/state but do not emit external rewards yet.
- See `docs/daily_login_bonus/xp_grant/milestone_options.md` for milestone handling options and the current XP-only/flag stance.
- Eligibility/preview should surface upcoming streak milestones (days to next milestone) and season boundary notes (days until reset) alongside projected XP.

- All arithmetic uses integers for day indices and decimals for multipliers; avoid floating-point drift.

## Concurrency & Idempotency

- Use the award uniqueness on `(user_id, reward_day_id)` to guard concurrent claims. Streak transition should be computed once; retries reuse stored award/receipt and do not mutate streak state again. (Wiring into claim/eligibility flows is deferred until orchestration exists.)
- Transition function must be re-runnable safely against the same inputs to support dry-run/preview APIs.

## Observability & Testing

- Logs/traces include: model type, prior/current streak, grace usage, season boundary checks, milestone hits, and chosen streak curve index.
- Metrics: streak_length histogram by model, grace_usage_total, milestone_unlock_total, and policy_version counters.
- Tests: unit/property tests per model covering happy path, grace/miss edges, DST boundary scenarios (with TR-03 reward-day resolution), and concurrency/idempotency under multi-claim simulation.
