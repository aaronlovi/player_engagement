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
| 6.3a | [ ] | **DBM In-Memory First** | For each missing write API, add it to `IPlayerEngagementDbmService`, implement it fully inside `PlayerEngagementDbmInMemoryService`/`PlayerEngagementDbmInMemoryData`, and add unit tests. Stub the Postgres implementation with `NotSupportedException` until statements exist. | Infrastructure unit tests (in-memory DBM). |
| 6.3b | [ ] | **Postgres Statements** | After an in-memory method works, create the matching Postgres statement (one insert/update per class) using the Identity statement pattern. Ensure all identifier columns (`policy_version`, `boost_id`, etc.) are `BIGINT` and values come from `DbmService.GetNextId64()`. | Statement-focused tests or targeted integration checks. |
| 6.3c | [ ] | **DbmService Wiring** | Replace the stubs in `PlayerEngagementDbmService` so each method executes its statement(s), mirrors in-memory behavior, and returns the proper `Result`. Keep tests covering both implementations. | Service tests exercising success/conflict paths. |
| 6.3d | [ ] | **Controller Enablement** | Once all write APIs are in place, update the controller POST routes to call them (via the slim persistence service) and return real responses instead of `501`. | Controller/integration tests. |
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

1. **DBM In-Memory First** – For every missing write API, add the signature to `IPlayerEngagementDbmService`, implement it entirely inside `PlayerEngagementDbmInMemoryService`/`PlayerEngagementDbmInMemoryData`, and cover it with unit tests. Stub the same method in `PlayerEngagementDbmService` using `NotSupportedException` so the compiler enforces the later work.
2. **Postgres Statements** – After an in-memory method works, create the corresponding Postgres statement class (one insert/update per file) using the Identity repository pattern (explicit SQL template, `NpgsqlParameter` bindings, clear result handling). All identifier columns—including `policy_version`, `boost_id`, and any future surrogate keys—must be `BIGINT`, so update `V002__policy_as_data.sql` (and DTOs) wherever `int` is still used.
3. **DbmService Wiring** – Replace the Postgres stubs with real implementations that execute the statements created in step 2, call `DbmService.GetNextId64()` for every identifier, and mirror the validation logic from the in-memory path. Add/extend tests to exercise both implementations.
4. **Controller Enablement** – Once the write APIs exist, update the controller POST routes to call them (via the slim persistence service) and return real responses instead of `501`.
5. **Testing** – After each sub-step, run `dotnet test` to keep regressions out. Statement and DBM tests must cover success/failure cases (duplicate drafts, invalid state transitions, timestamp guards).

#### 6.3 Function-by-Function Plan (apply steps 6.3a → 6.3c per method)

1. **CreatePolicyDraftAsync (`Result<long>`)**
   - *6.3a*: Add method to the interface. In memory, ensure the policy shell exists, generate a new `long` `policy_version` via `GetNextId64()`, insert the draft version, and replace streak/seasonal data. Unit-test happy path and duplicate drafts.
      - ✅ Implemented in `PlayerEngagementDbmInMemoryService`/`PlayerEngagementDbmInMemoryData` with coverage in `PlayerEngagementDbmInMemoryServiceTests`. Postgres support (6.3b/6.3c) still pending.
   - *6.3b*: Add statements `EnsurePolicyShellStmt`, `InsertPolicyVersionStmt`, `ReplacePolicyStreakCurveStmt`, and `ReplacePolicySeasonalBoostsStmt`. All IDs are `BIGINT`.
   - *6.3c*: Implement the Postgres method to run these statements within a transaction and return the generated version.

2. **PublishPolicyVersionAsync**
   - *6.3a*: In memory, allow `Draft/Archived → Published`, set `effective_at`/`published_at`, archive any prior published version (`superseded_at`), and apply segment overrides.
   - *6.3b*: Create `PublishPolicyVersionStmt`, `ArchiveCurrentPublishedStmt`, and `ReplacePolicySegmentOverridesStmt`.
   - *6.3c*: Wire up the Postgres method to run the statements with optimistic concurrency checks.

3. **RetirePolicyVersionAsync**
   - *6.3a*: In memory, ensure the version is Published and set `status='Archived'` plus `superseded_at=retiredAt`.
   - *6.3b*: Create `RetirePolicyVersionStmt`.
   - *6.3c*: Implement the Postgres method and map `rowsAffected` to success/conflict results.

4. **ReplacePolicyStreakCurveAsync / ReplacePolicySeasonalBoostsAsync**
   - *6.3a*: Replace the in-memory dictionaries. For seasonal boosts, assign new `boost_id` values via `GetNextId64()` whenever entries are recreated.
   - *6.3b*: Create `ReplacePolicyStreakCurveStmt` and `ReplacePolicySeasonalBoostsStmt` (delete + multi-row insert, binding `BIGINT` IDs).
   - *6.3c*: Implement the Postgres methods that run delete + insert within one transaction.

5. **UpsertPolicySegmentOverridesAsync**
   - *6.3a*: Replace the in-memory overrides dictionary.
   - *6.3b*: Create `ReplacePolicySegmentOverridesStmt` (delete + insert).
   - *6.3c*: Execute the statement and propagate errors.

6. **(Optional) Additional writes** – Revisit list/history APIs once write flows exist to ensure draft/published metadata surfaces correctly.

**Implementation notes**

- Always use `DbmService.GetNextId64()` for surrogate identifiers (`policy_version`, `boost_id`, etc.) and ensure the schema defines those columns as `BIGINT`. Since the database can be recreated, updating `V002__policy_as_data.sql` is acceptable.
- Statements must follow the Identity pattern (single responsibility, clear parameter binding, explicit result handling).
- Extend `PlayerEngagementDbmInMemoryData` with helper methods such as `EnsurePolicy`, `InsertVersion`, `MarkPublished`, `ReplaceStreakCurve`, `ReplaceSeasonalBoosts`, and `ReplaceSegmentOverrides` so in-memory behavior stays aligned with SQL.
- Controllers will continue to reload documents through `PolicyDocumentPersistenceService` once the write APIs return real results.

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
