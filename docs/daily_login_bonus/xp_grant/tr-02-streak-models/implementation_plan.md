# TR-02 — Streak Models Implementation Plan

| # | Done | Step | Exit Criteria |
| - | - | - | - |
| 1 | [ ] | Confirm scope and inputs/outputs for the streak engine (policy version, streak state, reward-day id, claim timestamp). | Shared interface signed off; aligns with TR-03 reward-day resolution and TR-06 streak state shape. |
| 2 | [ ] | Document formal transition rules per model (Plateau/Cap, Weekly Cycle Reset, Decay/Soft Reset, Tiered Seasonal Reset, Milestone Meta-Reward), including grace and miss handling. | Written rules captured in code comments/tests; edge cases (miss gaps, season boundaries, milestone repeat prevention) resolved. |
| 3 | [ ] | Define/implement the streak engine contract (pure function/service) and model-state schema (what goes into `model_state`). | Interface and DTOs in Domain/Shared; deterministic output with no side effects. |
| 4 | [ ] | Implement Plateau/Cap transition logic and map to streak curve indices; add unit/property tests. | Tests cover plateau day, post-plateau claims, misses with/without grace. |
| 5 | [ ] | Implement Weekly Cycle Reset transition logic with fixed 7-day cycle; add tests. | Tests cover cycle rollover, grace-protected gaps, and reset behavior. |
| 6 | [ ] | Implement Decay/Soft Reset transition logic (decay percent + grace day rules) using floor+clamp to 1 for decay rounding; add tests. | Tests cover decay rounding (floor to int, min 1), multiple consecutive misses, and grace interactions. |
| 7 | [ ] | Implement Tiered Seasonal Reset logic (season boundaries + tier selection); add tests. | Tests cover tier selection, non-overlap, season transition resets, and seasonal metadata handling. |
| 8 | [ ] | Implement Milestone Meta-Reward handling (unlock tracking, idempotent milestones); add tests. | Tests cover milestone triggering, duplicate prevention, and milestone flags/events. |
| 9 | [ ] | Wire engine into eligibility/claim flows; ensure idempotent award path uses stored streak/receipt on retries. | Claims/eligibility return consistent streak/XP outputs across all models; concurrency tests pass against award uniqueness. |
| 10 | [ ] | Add observability hooks (logs/metrics) for streak decisions and milestone/season events. | Metrics and structured logs present in claim/eligibility spans; dashboards updated if needed. |
| 11 | [ ] | Validate policy/DTO mappings and schema (streak_model_parameters, curve rows) align with engine expectations. | Host validators enforce model-specific parameters; persistence round-trips typed models correctly. |
| 12 | [ ] | Run test suites (unit/property/integration) and update runbooks/docs for operator previews and debugging. | CI passes; docs refreshed with model behaviors and troubleshooting tips. |
