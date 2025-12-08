# TR-02 Streak Models Implementation Plan

## Scope

- Translate docs/daily_login_bonus/xp_grant/tr-02-streak-models/implementation_plan.md (steps 1–12) into executable work for the streak engine and integrations.
- Align with TR-02 high/low-level requirements, soft_decay_streak_model.md, milestone_options.md, XP grant HLD/technical/business requirements, and AGENTS.md constraints (pure functions, no LINQ in prod, explicit usings, XML docs for new public members, UTC timestamps, policy-version awareness, no Dapper).

## Inputs / Outputs (engine contract)

- Inputs: policy version (model type + parameters + streak curve rows), prior streak state (current_streak, longest_streak, grace_used, last_reward_day_id, model_state JSON), reward-day id, claim timestamp.
- Outputs: new streak state, XP multiplier/additive bonus derived from streak curve and model logic, milestone/season notes, receipt-ready fields for claim/eligibility, observability breadcrumbs (model type, curve index, grace/decay/season/milestone decisions).
- Determinism/idempotency: transition must be pure for the given inputs; retries reuse stored award and do not mutate state again.

## Dependencies

- Policy storage/validation: streak_model_type + streak_model_parameters schema; streak curve table; season metadata from SeasonGrain/Dbm.
- Persistence: daily_login_bonus_xp_streaks (TR-06) model_state JSON; daily_login_award uniqueness on (user_id, reward_day_id); xp_ledger append-only.
- Time: reward-day resolution per TR-03 (anchor TZ + claim window) provided by orchestrator.
- Observability: metrics/log hooks in claim/eligibility flows; milestone flags per milestone_options.md.

## Work Plan (mapped to steps 1–12)

1) Confirm engine scope & contract  
   - Draft interface/DTOs capturing inputs/outputs above; align with reward-day resolver contract.  
   - Validate model_state shape expectations and serialization.
2) Formalize transition rules per model  
   - Write rule docs/comments for Plateau/Cap, Weekly Cycle Reset, Decay/Soft Reset (floor+clamp), Tiered Seasonal Reset (hard boundary, no carry), Milestone Meta-Reward (XP + flag).  
   - Resolve grace/miss sequencing (grace first, capped by policy, no cross-season grace).
3) Define engine contract & model_state schema  
   - Add domain/shared interfaces/types; ensure pure-function signature; define model_state JSON schema per model (e.g., decay counters, season ids, milestone flags).  
   - Include XML docs for public types/members.
4) Implement Plateau/Cap logic + tests  
   - Increment until plateau day then clamp; apply plateau multiplier thereafter.  
   - Tests: plateau reach, post-plateau claims, misses with/without grace.
5) Implement Weekly Cycle Reset + tests  
   - Fixed 7-day cycle reset to day 1 on rollover; grace only prevents breakage within window.  
   - Tests: cycle rollover, grace-protected gap, reset after miss beyond grace.
6) Implement Decay/Soft Reset + tests  
   - After grace exhausted, apply decayPercent: next = max(1, floor(current_streak * (1 - decayPercent))).  
   - Tests: rounding floor, multi-miss decay, grace interactions, clamp to 1.
7) Implement Tiered Seasonal Reset + tests  
   - SeasonGrain authoritative start/end; reload at season end; fallback to policy metadata.  
   - Reset to day 1 on first claim after season end; tiers non-overlapping; no grace across seasons.  
   - Tests: tier selection, boundary reset, grain reload, metadata fallback.
8) Implement Milestone Meta-Reward + tests  
   - Track milestone hits in model_state and award metadata; prevent duplicate on retries.  
   - Tests: milestone triggering, duplicate prevention, coexistence with streak curve XP.
9) Wire engine into eligibility/claim flows  
   - Use award uniqueness; retries return stored receipt/state.  
   - Ensure eligibility uses same pure transition logic without writes; surface milestone/season notes.
10) Add observability hooks  
    - Structured logs/traces for model decisions; metrics: streak_length histogram by model, grace_usage_total, milestone_unlock_total, policy_version counters; latency/error metrics already in host.  
    - Include policy_version, receipt_id, reward_day_id in logs.
11) Validate policy/DTO mappings & schema  
    - Enforce model-specific parameter validation; reject unknown model types; ensure streak curve caps/tiers non-overlap.  
    - Ensure serialization/deserialization of model_state, season ids, milestone flags.
12) Tests/docs/runbooks  
    - Unit/property tests per model (miss gaps, DST via TR-03 resolver, concurrency/idempotency sim).  
    - Update docs/operator notes for previews (milestone/season messaging), troubleshooting.  
    - Record what to run: dotnet test src/PlayerEngagement.sln (owner executes).

## Observability & Testing Expectations

- Logs: model type, prior/current streak, grace usage, decay/season boundary checks, milestone hits, curve index chosen, policy_version, receipt_id. JSON structured.  
- Metrics: claims_total/already_claimed_total/xp_granted_sum (existing), plus streak_length_hist by model, grace_usage_total, milestone_unlock_total, policy_version_count.  
- Tests: xUnit, no FluentAssertions; property tests for streak transitions; concurrency/idempotency simulation; DST/timezone cases through reward-day resolver.

## Deliverables

- Engine interfaces/types with XML docs; model implementations and tests per model; updated claim/eligibility wiring; observability plumbing; policy validation updates; documentation/runbook updates.  
- Plan to store outputs under respective projects (Domain/Infrastructure/Host) per AGENTS namespace guidance.

## Open Questions / Risks

- Exact season data source freshness and cache invalidation timing for SeasonGrain.  
- Milestone schema stability (RewardType/RewardValue strings) until asset catalog exists.  
- Potential need for additional property tests for extreme decay percentages or large streaks.

## Metadata

### Status

success

### Confidence

High; scope and requirements are well-documented.

### Dependencies

- AGENTS.md
- docs/daily_login_bonus/xp_grant/tr-02-streak-models/implementation_plan.md
- TR-02 high/low-level requirements, soft_decay_streak_model.md, milestone_options.md
- XP grant business/technical/HLD docs

### Open Questions

- SeasonGrain data freshness/caching specifics.
- Milestone RewardType/RewardValue final shapes when non-XP assets arrive.

### Assumptions

- Reward-day resolution per TR-03 is already available to the engine.  
- Policy storage and SeasonGrain interfaces can be extended without schema conflicts.
