# Step 2 — Domain Definition

## Objective

Shape the canonical domain model for policy documents so every service and UI component references the same fields and semantics.

## Inputs

- Findings from Step 1 discovery.
- Business requirements table (`docs/xp_grant/xp_grant_business_requirements.md`).
- Glossary definitions (`docs/xp_grant/xp_grant_glossary.md`).
- HLD policy component section (`docs/xp_grant/xp_grant_high_level_design.md`).

## Tasks

- [x] Draft a domain model describing policy aggregates (base XP, streak curves, grace configuration, claim window, streak model, seasonal boosts, segment overrides).
- [x] Define immutability and versioning rules (publish vs. draft states, effective/expiration timestamps).
- [x] Specify validation rules (e.g., streak model enum values, allowable claim window ranges, seasonal multiplier caps).
- [x] Review model with product/game design stakeholders to confirm business semantics. *(Confirmed with current operator/developer; future stakeholders TBD.)*
- [x] Record decisions in a lightweight ADR or design note linked from implementation plan.

## Summary

- `docs/xp_grant/tr-01-policy-as-data/domain_model.md` documents the `PolicyAggregate`, its sub-components (base award, streak curve, grace policy, claim window, streak model enums, seasonal boosts, segment overrides), and serialization approach. The model assumes JSONB storage to keep iteration lightweight during local development.
- Versioning: each `policyKey` supports multiple versions with statuses `Draft`, `Published`, and `Archived`. Publishing enforces single active version semantics and immutability of past versions.
- Validation rules include sequential streak curve days, multiplier/bonus bounds, non-overlapping seasonal boosts, grace configuration constraints, and segment reference checks. XP caps default to 5000 XP per claim (configurable constant to be finalized during implementation).
- Serialization strategy leverages System.Text.Json with schema validation. Policy payloads are stored in `xp_rules.definition`, aligning with existing migration shell.

## Deliverables

- Domain model specification: `docs/xp_grant/tr-01-policy-as-data/domain_model.md`
- Outstanding question logged regarding milestone reward inventory integration; to be addressed when cosmetic economy details are defined.

## Open Questions

- Milestone rewards may depend on external cosmetic or currency systems. Determine integration requirements before implementing milestone model payouts.
- Segment catalog source remains TBD until Step 7 defines resolver and data source.

## References

- `AGENTS.md` coding and documentation guidelines.
- `docs/game-engagement-mechanics.md` and `docs/daily_login_bonus.md` for user experience rationale.
