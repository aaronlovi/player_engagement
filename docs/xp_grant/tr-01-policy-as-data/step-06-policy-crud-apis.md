# Step 6 — Policy CRUD APIs

## Objective
Expose versioned policy management endpoints that enable operators and tooling to create, review, and publish policy documents while preserving immutability.

## Inputs
- Domain model and validation rules from Step 2.
- Repository interfaces from Step 5.
- API conventions in `PlayerEngagement.Host` and related infrastructure.
- Admin workflow requirements in `docs/xp_grant/xp_grant_business_requirements.md` (BR-13) and technical requirements (TR-01, TR-10, TR-22).

## Tasks
- [ ] Define API contracts (REST or gRPC) for creating drafts, publishing versions, retrieving versions/history, and retiring policies.
- [ ] Implement controllers/service grains that leverage repository layer and enforce immutability (no edits after publish).
- [ ] Add request validation (model enums, claim window ranges, seasonal multiplier limits).
- [ ] Generate/update OpenAPI or proto definitions and publish to shared documentation.
- [ ] Create automated tests (unit + integration) covering CRUD flows, validation errors, and history retrieval.

## Deliverables
- Policy API implementation with tests.
- Updated API documentation/specification in repo or shared docs site.

## References
- `AGENTS.md` for coding standards and testing expectations.
- `docs/xp_grant/xp_grant_high_level_design.md` (Policy Service component interactions).

## Open Questions
- Do we support soft delete or only publish/retire semantics?
- Are there role-based access controls required before exposing these endpoints?
