# TR-02 Streak Models Implementation Notes

## Work done
- Added streak engine contract and types (state, context, result) plus runtime model state placeholders.
- Implemented initial `StreakEngine` with Plateau/Cap, Weekly Cycle reset, Decay scaffold, and milestone tracking stub; grace-first handling and streak curve lookup included; XP calculation rounds away from zero.
- Added new test project `PlayerEngagement.Domain.Tests` to solution and covered Plateau/Cap behaviors with xUnit tests (consecutive growth, plateau clamp, grace-covered miss, and reset beyond grace).

## Next steps (per plan)
- Flesh out Decay/Soft Reset rules for multi-miss scenarios and model-specific state.
- Implement Tiered Seasonal Reset logic with SeasonGrain integration and tier multiplier selection.
- Implement Milestone Meta-Reward state persistence and observability hooks; extend metrics/logging.
- Wire engine into eligibility/claim flows, policy/DTO validation, and persistence of streak/model_state.
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
- Detailed seasonal boundary handling (SeasonGrain reload cadence, tier selection) and milestone schema for future non-XP rewards.
### Assumptions
- Current XP calculation (round away from zero) and streak curve selection (last entry at/under effective day) align with intended behavior; adjust if policy rules differ.
