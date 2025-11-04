# Step 7 — Segment Resolution Integration

## Objective
Wire the policy-as-data system into the daily login claim flow so each claim selects the correct policy version based on player segment and records the version on the XP ledger.

## Inputs
- Policy repositories/APIs from Steps 5–6.
- Current claim orchestration logic discovered in Step 1.
- Segmentation rules defined in `docs/xp_grant/xp_grant_business_requirements.md` (BR-07) and technical requirements (TR-10).

## Tasks
- [ ] Implement or extend segment resolver to map player attributes to policy ids at claim time (with logging/audit).
- [ ] Update claim orchestrator to fetch policy version details and pass them into streak/XP calculation flows.
- [ ] Ensure claim persistence (daily login award, XP ledger entries) includes `policy_version`.
- [ ] Validate idempotent claim behavior remains intact with the new dependency chain.
- [ ] Add automated tests covering multi-segment scenarios, fallback/default policies, and concurrency.

## Deliverables
- Updated claim service code referencing policy-as-data.
- Test suite results demonstrating correct policy selection and idempotent behavior.

## References
- `docs/xp_grant/xp_grant_high_level_design.md` (Daily Login Service, Policy Service, Streak Engine interactions).
- `docs/xp_grant/xp_grant_technical_requirements.md` (TR-03, TR-04, TR-07, TR-08, TR-10).

## Open Questions
- How do we handle players without a segment (default policy) or with multiple overlapping segments?
- Should segment assignment be cached, or recalculated for every claim?
