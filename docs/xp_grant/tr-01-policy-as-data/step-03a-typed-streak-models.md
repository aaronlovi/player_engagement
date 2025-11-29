# Follow-up: Strongly Typed Streak Models Implementation Plan

This plan extends `Step 3 — Data Modeling` to replace the loose `StreakModelDefinition.Parameters` dictionary with strongly typed domain models and mapper validation.

| # | Status | Task | Notes / Deliverable |
| --- | --- | --- | --- |
| 1 | [x] | PlateauCap model: domain type + validation + mapper/test updates. | Record with `PlateauDay`, `PlateauMultiplier`; add parser tests and Host validator coverage for this model. |
| 2 | [x] | WeeklyCycleReset model: domain type + validation + mapper/test updates. | Fixed 7-day cycle; no parameters required. Add parser tests and Host validator coverage for this model. |
| 3 | [x] | DecayCurve model: domain type + validation + mapper/test updates. | Record with `DecayPercent`, `GraceDay`; add parser tests and Host validator coverage for this model. |
| 4 | [x] | TieredSeasonalReset model: domain type + validation + mapper/test updates. | Record with tier collection `{startDay,endDay,bonusMultiplier}` and overlap checks; parser/validator tests. |
| 5 | [x] | MilestoneMetaReward model: domain type + validation + mapper/test updates. | Record with milestones `{day,rewardType,rewardValue}`; parser/validator tests. |
| 6 | [ ] | Update `StreakModelDefinition` to wrap typed variants (discriminated union). | Keep backward compatibility helpers if needed; document serialization expectations. |
| 7 | [ ] | Shared validation utilities for ranges/overlaps as needed. | Place in `PlayerEngagement.Shared` with unit tests. |
| 8 | [ ] | Update DTO mapping/tests to reflect typed models (no DB schema change). | Adjust Infrastructure tests to assert typed model instances and properties. |
| 9 | [ ] | Extend Host request DTOs/validators to enforce model-specific parameters. | Update policy CRUD inputs and add Host tests for each model’s payloads. |
| 10 | [ ] | Ensure controller-to-persistence flow handles typed models end-to-end. | Verify serialization into DB JSON matches typed definitions; add integration-style tests for CRUD paths. |
| 11 | [ ] | Documentation update summarizing typed model shapes and validation rules. | Append to `step-03-data-modeling.md` or within this follow-up. |
| 12 | [ ] | Build/test validation after changes. | Run `dotnet build` and relevant `dotnet test` suites (Domain/Infrastructure/Shared/Host). |

## Numeric Handling Notes

- Host request DTOs use `int/long` for day/count fields and `decimal` for multipliers/percentages to avoid float drift; model validation rejects non-integers where integers are required.
- Mapper parsing treats JSON numbers as `long` when integral, otherwise `decimal`, and applies per-model range checks (e.g., positive days, 0–1 decay percent).
- If UI sends numeric strings, enable `AllowReadingFromString` on JSON options or per-property converters; still validate ranges to fail fast on bad input.

## Model Invariants (by streak model)

- PlateauCap: `PlateauDay >= 1`; `PlateauMultiplier > 0`; day indices must be integers.
- WeeklyCycleReset: fixed 7-day cycle; integer day/count semantics; resets on reward-day boundaries; no parameters accepted.
- DecayCurve: `DecayPercent` within `[0,1]`; `GraceDay >= 0`; integer day/count semantics.
- TieredSeasonalReset: tiers require `StartDay >= 1`, `EndDay >= StartDay`, `BonusMultiplier > 0`; tiers must not overlap; gaps are allowed; integer day/count semantics. Treat ranges as inclusive for both start/end when evaluating days.
- MilestoneMetaReward: each milestone `Day >= 1`; `RewardType` and `RewardValue` non-empty; integer day semantics. Future integration with inventory may refine reward validation.
