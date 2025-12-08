# TR-02 Streak Models Implementation Notes

## Work done

- Added streak engine contract and types (state, context, result) plus runtime model state placeholders.
- Implemented `StreakEngine` with Plateau/Cap, Weekly Cycle reset, Decay, Tiered Seasonal tier selection/boundary reset, and milestone tracking stub; grace-first handling and streak curve lookup included; XP calculation rounds away from zero.
- Added logging guardrails for missing model configuration (Plateau, Seasonal, Milestone) and milestone hit info logs using a typed logger.
- Added new test project `PlayerEngagement.Domain.Tests` to solution and covered Plateau/Cap, Decay, and Tiered Seasonal behaviors with xUnit tests (growth, clamp, grace-covered miss, decay multi-miss, tier multipliers, season boundary reset).
- Introduced test factory helpers for policy documents to reduce direct `new` usage per testing guideline.

## Next steps (per plan)

- Implement Milestone Meta-Reward state persistence and observability hooks; extend metrics/logging beyond current logs.
- Wire engine into eligibility/claim flows, policy/DTO validation, and persistence of streak/model_state (deferred until orchestration exists).
- Update docs/runbooks and have owner run `dotnet test src/PlayerEngagement.sln`.

## Metadata

### Status

partial

### Confidence

Medium — foundational engine and Plateau/Cap logic are in place; remaining models and wiring still pending.

### Dependencies

- AGENTS.md
- docs/daily_login_bonus/xp_grant/tr-02-streak-models/.prompts/streak-models-plan/plan.md
- TR-02 requirements and supporting docs
- New types/tests under src/PlayerEngagement.Domain and src/PlayerEngagement.Domain.Tests

### Open Questions

- SeasonGrain integration details (reload cadence, authoritative boundaries) and milestone schema for future non-XP rewards.

### Assumptions

- Current XP calculation (round away from zero) and streak curve selection (last entry at/under effective day) align with intended behavior; adjust if policy rules differ.
