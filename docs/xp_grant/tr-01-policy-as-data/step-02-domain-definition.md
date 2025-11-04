# Step 2 — Domain Definition

## Objective
Shape the canonical domain model for policy documents so every service and UI component references the same fields and semantics.

## Inputs
- Findings from Step 1 discovery.
- Business requirements table (`docs/xp_grant/xp_grant_business_requirements.md`).
- Glossary definitions (`docs/xp_grant/xp_grant_glossary.md`).
- HLD policy component section (`docs/xp_grant/xp_grant_high_level_design.md`).

## Tasks
- [ ] Draft a domain model describing policy aggregates (base XP, streak curves, grace configuration, claim window, streak model, seasonal boosts, segment overrides).
- [ ] Define immutability and versioning rules (publish vs. draft states, effective/expiration timestamps).
- [ ] Specify validation rules (e.g., streak model enum values, allowable claim window ranges, seasonal multiplier caps).
- [ ] Review model with product/game design stakeholders to confirm business semantics.
- [ ] Record decisions in a lightweight ADR or design note linked from implementation plan.

## Deliverables
- Domain model document (class diagram or structured Markdown) ready for implementation.
- Approved glossary-aligned definitions for each policy field.
- List of outstanding questions to resolve before data modeling (if any).

## References
- `AGENTS.md` coding and documentation guidelines.
- `docs/game-engagement-mechanics.md` and `docs/daily_login_bonus.md` for user experience rationale.

## Open Questions
- Do we support draft policies that are never published, and how long should drafts persist?
- Are seasonal boosts modeled as separate feature flags or embedded policy fields?
