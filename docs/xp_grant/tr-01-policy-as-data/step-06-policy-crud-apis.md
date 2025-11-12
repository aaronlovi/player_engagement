# Step 6 — Policy CRUD APIs

## Objective
Expose versioned policy management endpoints that enable operators and tooling to create, review, and publish policy documents while preserving immutability.

## Inputs
- Domain model and validation rules from Step 2.
- Repository interfaces from Step 5.
- API conventions in `PlayerEngagement.Host` and related infrastructure.
- Admin workflow requirements in `docs/xp_grant/xp_grant_business_requirements.md` (BR-13) and technical requirements (TR-01, TR-10, TR-22).

## Tasks
| Step | Status | Focus | Key Work Items | Test Strategy |
| --- | --- | --- | --- | --- |
| 6.1 | [x] | **Host Wiring** | Add `MapControllers()`/required middleware to `PlayerEngagement.Host`, ensure REST endpoints resolve dependencies (dbm services, persistence). | Manual: run host and hit stub controller (`/xp/policies/ping`). |
| 6.2a | [x] | **Endpoint Inventory** | Enumerate CRUD operations (create draft, publish w/ future effective, fetch version/history, retire) and define request/response fields + status codes. | Document review; ensure OpenAPI draft matches Step 2 specs. |
| 6.2b | [x] | **Controller & Routing Layout** | Added concrete `PoliciesController` routes for every CRUD/segment endpoint with typed request payloads wired under `/xp/policies`. Controllers log each call and currently return `501` until Step 6.3 provides implementations. | Manual verification via stub routes + unit tests once implemented. |
| 6.2c | [x] | **Validation & Contract Docs** | Added `PolicyRequestValidator` enforcing key rules (policy key regex, streak curves sequential, seasonal boost overlap detection, publish window tolerance, segment override guardrails). Controllers call it and return RFC7807 validation responses; request DTO XML docs describe payloads. | `PlayerEngagement.Host.Tests` project exercises validator happy/negative paths. |
| 6.3a | [ ] | **Write Statements** | Add Postgres statements for draft creation, version publish (with optional future effective), version retire, streak/seasonal persistence, and segment override upserts. Mirror the pattern used in `Identity.Infrastructure` statements such as `AddUserStmt` and `UpdateUserProfileStmt` (parameter binding, optimistic concurrency, returning IDs). | Statement unit tests under `PlayerEngagement.Infrastructure.Tests` using in-memory DBM. |
| 6.3b | [ ] | **DBM Surface** | Extend `IPlayerEngagementDbmService` plus `PlayerEngagementDbmService`/`PlayerEngagementDbmInMemoryService` with async methods that execute the new statements (create draft, publish, retire, upsert overrides, replace streak curve + seasonal boosts). Ensure in-memory store mirrors SQL behavior for tests. | Service tests exercising success/conflict paths. |
| 6.3c | [ ] | **Host Orchestration** | Introduce a thin command orchestrator (or controller logic) that invokes the new DBM methods via `PolicyDocumentPersistenceService`, enforces immutability checks, and updates controllers so POST routes return real results instead of `501`. | Unit tests for orchestrator + controller integration tests once statements exist. |
| 6.4 | [ ] | **Validation Layer** | Configure FluentValidation/manual validators for enums, claim windows, seasonal multipliers, streak parameters, ensuring errors surface via consistent problem details. | Unit tests for validators (including boundary/error cases). |
| 6.5 | [ ] | **OpenAPI Documentation** | Update generated swagger/OpenAPI (e.g., Swashbuckle) with policy endpoints including sample payloads and retire-only semantics. | Manual review of generated Swagger UI/spec. |
| 6.6 | [ ] | **Testing** | Unit tests for controllers/services (happy path, validation errors, immutability), integration smoke tests covering publish + future-effective activation via in-memory DBM. | `dotnet test` (unit) + targeted integration smoke tests. |

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
- **Write persistence gap**: DB statements for creating/publishing/retiring policy versions have not been implemented yet, so the corresponding endpoints still return `501 Not Implemented`.
- **6.2a REST Endpoint Inventory**

### Step 6.3 Execution Plan (DB Write Path)

1. **Statement Layer** – Implement `CreatePolicyDraftStmt`, `PublishPolicyVersionStmt`, `RetirePolicyVersionStmt`, `UpsertPolicySegmentOverridesStmt`, plus helper statements for streak curve and seasonal boost bulk inserts. Follow the same style as `Identity.Infrastructure` statements (e.g., `AddUserStmt`, `UpdateUserProfileStmt`, `UpdateUserStatusStmt`) by inheriting from the appropriate `Postgres*DbStmtBase`, binding parameters via `NpgsqlParameter`, and returning affected rows/IDs for optimistic concurrency feedback.
2. **DBM Integration** – Extend `IPlayerEngagementDbmService` and both the Postgres and in-memory implementations with asynchronous methods that execute the new statements. Guard against duplicate drafts, enforce publish status transitions, and make sure the in-memory backing store mirrors SQL semantics so existing tests can exercise write paths without Postgres.
3. **Application Orchestration** – Once DBM writes exist, introduce/update a thin command service that: (a) validates inputs (reusing `PolicyRequestValidator` where applicable), (b) calls the DBM write APIs, (c) reloads the resulting `PolicyDocument` via `PolicyDocumentPersistenceService`, and (d) surfaces those results through the controller POST endpoints. This will retire the current `501` placeholders for create/publish/retire/segment override routes.
4. **Testing** – Add statement-level tests (similar to existing reader tests) plus new orchestration tests to cover success and failure cases (duplicate drafts, publish conflicts, invalid retire timestamps). Ensure `dotnet test` exercises both infrastructure and host layers.

| Endpoint | Purpose | Request Body (key fields) | Response | Status Codes |
| --- | --- | --- | --- | --- |
| `POST /xp/policies/{policyKey}/versions` | Create a new draft version for a policy (policy auto-creates on first draft). | `displayName`, `description`, `baseXpAmount`, `currency`, `claimWindowStartMinutes`, `claimWindowDurationHours`, `anchorStrategy`, `graceAllowedMisses`, `graceWindowDays`, `streakModelType`, `streakModelParameters`, `previewSampleWindowDays`, `previewDefaultSegment`, `seasonalBoosts[]`, optional `effectiveAt`. | Draft representation (`policyKey`, generated `policyVersion`, status=`Draft`). | `201 Created` (Location header to version URI), `400` validation errors, `409` if draft already exists. |
| `POST /xp/policies/{policyKey}/versions/{version}/publish` | Publish a draft or archived version, optionally scheduling a future go-live. | `effectiveAt` (optional UTC timestamp), `segmentOverrides` (optional map segment→version). | `202 Accepted` with published version summary (`policyKey`, `policyVersion`, `status`, `effectiveAt`). | `202` success, `404` missing policy/version, `409` if already published/retired, `422` invalid schedule (past effective date). |
| `POST /xp/policies/{policyKey}/versions/{version}/retire` | Retire an existing published version (no delete). | `retiredAt` (optional; defaults now). | `200 OK` with retired metadata (`retiredAt`). | `200` success, `404` not found, `409` if version already retired or never published. |
| `GET /xp/policies/{policyKey}/versions/{version}` | Retrieve a single policy version (any status). | n/a | Full `PolicyDocument` payload (version info + streak curve + seasonal boosts). | `200` success, `404` missing. |
| `GET /xp/policies/{policyKey}/versions` | List versions with optional filters (`status`, `effectiveBefore`, `limit`). Provides history for operators. | Query params | `200 OK` with array of lightweight version summaries (key, version, status, effectiveAt, publishedAt). | `200`, `404` if policy key unknown. |
| `GET /xp/policies/active?policyKey=...` | Fetch the currently active version (respecting effective_at, server UTC). | Query `policyKey` (future: segment). | `200 OK` with `PolicyDocument`, `404` when no active version exists, `409` if multiple scheduled versions overlap (validation bug). |
| `GET /xp/policies/{policyKey}/segments` & `PUT` same | Read/update segment overrides (map of segment key → version). `PUT` enforces retire-only semantics by requiring explicit version numbers. | For `PUT`: `{ "overrides": { "segmentKey": version } }`. | `200 OK` returning resulting map. | `200` success, `400` invalid data, `404` policy missing. |

- Request/response schemas will mirror Step 2 domain types; all timestamps are UTC ISO-8601. Errors use RFC 7807 problem details with `traceId`.
