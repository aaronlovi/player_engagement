# Step 8 — Admin Console Updates

## Objective

Enhance the Angular admin interface so live-ops teams can manage policy versions end-to-end within `ui/player-engagement-config-ui`.

## Inputs

- Policy API contracts from Step 6.
- Current admin UI features documented in `ui/player-engagement-config-ui/README.md`.
- Design requirements from business docs (BR-13, BR-14).

## Tasks

- [ ] Integrate new API endpoints for listing, creating, previewing, publishing, and retiring policies.
- [ ] Design UI components/forms for editing policy fields, including streak curve and claim window inputs.
- [ ] Add validation and user feedback (error toasts, success messages, preview calculations).
- [ ] Provide policy diff/history views to visualize version changes.
- [ ] Implement authorization checks (route guards) if operator roles vary.
- [ ] Update UI unit/e2e tests to cover new workflows.

## Deliverables

- Updated Angular components/services with associated tests.
- Documentation snippet (in README or dedicated doc) explaining operator workflow.

## References

- `AGENTS.md` notes on front-end placement within project structure.
- `docs/daily_login_bonus.md` for context when designing preview messaging.

## Open Questions

- Do operators require draft collaboration features (e.g., comments, staged approvals)?
- Should the UI visualize projected XP payouts for different streak models via charts?
