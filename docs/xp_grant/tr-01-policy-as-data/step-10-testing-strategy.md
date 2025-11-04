# Step 10 — Testing Strategy

## Objective
Establish automated and manual testing coverage that validates policy-as-data functionality across services and UI.

## Inputs
- New features from Steps 5–9.
- AGENTS workflow checklist (build/test expectations).
- Existing CI pipeline configurations for .NET and Angular projects.

## Tasks
- [ ] Define test suites: unit tests (policy serialization, repository behavior, streak calculations), integration tests (CRUD API, claim flow), end-to-end tests (operator creates policy → claim uses version).
- [ ] Update or create test fixtures/mocks for policy documents and segment assignments.
- [ ] Ensure CI runs `dotnet test` and `npm test` (plus e2e if applicable) with coverage thresholds aligned to team standards.
- [ ] Document manual regression steps for release candidates (policy creation, segment override, claim retry scenario).
- [ ] Track test gap remediation work (e.g., flaky tests, missing negative cases).

## Deliverables
- Updated automated tests committed to the repository.
- Testing playbook documenting suite locations, commands, and expected results.

## References
- `docs/xp_grant/xp_grant_technical_requirements.md` for acceptance criteria tied to validation.
- `docs/daily_login_bonus.md` to craft realistic streak scenarios for test data.

## Open Questions
- Should we integrate contract testing between Policy Service and Admin UI?
- Are load/performance tests required before production rollout?
