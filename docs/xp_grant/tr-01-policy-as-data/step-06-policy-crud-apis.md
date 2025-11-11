# Step 6 — Policy CRUD APIs

## Objective
Expose versioned policy management endpoints that enable operators and tooling to create, review, and publish policy documents while preserving immutability.

## Inputs
- Domain model and validation rules from Step 2.
- Repository interfaces from Step 5.
- API conventions in `PlayerEngagement.Host` and related infrastructure.
- Admin workflow requirements in `docs/xp_grant/xp_grant_business_requirements.md` (BR-13) and technical requirements (TR-01, TR-10, TR-22).

## Tasks
| Step | Status | Focus | Key Work Items |
| --- | --- | --- | --- |
| 6.1 | [ ] | **Host Wiring** | Add `MapControllers()`/required middleware to `PlayerEngagement.Host`, ensure REST endpoints resolve dependencies (dbm services, persistence). |
| 6.2a | [ ] | **Endpoint Inventory** | Enumerate CRUD operations (create draft, publish w/ future effective, fetch version/history, retire) and define request/response fields + status codes. |
| 6.2b | [ ] | **Controller & Routing Layout** | Choose controller structure (e.g., `PoliciesController` under `/xp/policies`) and identify service dependencies to be injected. |
| 6.2c | [ ] | **Validation & Contract Docs** | Outline API-level validation rules and specify OpenAPI/Swagger updates so consumers (Angular UI) understand payloads/errors. |
| 6.3 | [ ] | **Application Services** | Implement orchestrators/services that call `IPlayerEngagementDbmService`/`PolicyDocumentPersistenceService`, enforce immutability, and schedule future-effective publishes. |
| 6.4 | [ ] | **Validation Layer** | Configure FluentValidation/manual validators for enums, claim windows, seasonal multipliers, streak parameters, ensuring errors surface via consistent problem details. |
| 6.5 | [ ] | **OpenAPI Documentation** | Update generated swagger/OpenAPI (e.g., Swashbuckle) with policy endpoints including sample payloads and retire-only semantics. |
| 6.6 | [ ] | **Testing** | Unit tests for controllers/services (happy path, validation errors, immutability), integration smoke tests covering publish + future-effective activation via in-memory DBM. |

## Deliverables
- Policy API implementation with tests.
- Updated API documentation/specification in repo or shared docs site.

## References
- `AGENTS.md` for coding standards and testing expectations.
- `docs/xp_grant/xp_grant_high_level_design.md` (Policy Service component interactions).

## Decisions & Open Questions
- **Lifecycle semantics**: Retire-only (no soft delete). Implement policy states via statuses/effective windows.
- **RBAC**: Deferred while a single operator uses the system; track follow-up in TODO.md for future enforcement.
- **API style**: Use REST endpoints in `PlayerEngagement.Host`. We still need to add the HTTP routing/mvc configuration since only health stubs exist today. (Optional: evaluate gRPC later for service-to-service calls.)
