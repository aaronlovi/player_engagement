# Step 5 — Repository & Cache Layer

## Objective
Provide infrastructure services that load policy documents efficiently and expose them to domain consumers with caching and version awareness.

## Inputs
- Updated schema and entities from Steps 3–4.
- Existing repository patterns in `PlayerEngagement.Infrastructure`.
- Cache strategy guidelines (in-memory, distributed) used elsewhere in the project.

## Tasks
- [ ] Implement repositories or data access objects for policy retrieval by id, by version, and by segment assignment (see breakdown below).
- [ ] Add caching layers (e.g., memory cache, Redis) as appropriate, including invalidation rules when new versions publish.
- [ ] Define interfaces consumed by orchestrator/domain services to decouple persistence details.
- [ ] Write unit tests for repository behavior (happy path, cache hits, cache eviction, missing policy).
- [ ] Document performance considerations and metrics to observe (cache hit rate, latency).

## Deliverables
- Repository/cache implementation with accompanying tests in `PlayerEngagement.Infrastructure`.
- Documentation snippet describing API usage for downstream components.

## References
- `docs/xp_grant/xp_grant_high_level_design.md` (Policy Service component).
- `docs/xp_grant/xp_grant_technical_requirements.md` (TR-01, TR-08, TR-10).

## Open Questions
- Distributed cache coherence across Orleans silos? → Avoid for now; Orleans grains act as the caching boundary. Each grain reloads policy state from the database on activation, so distributed caches (Redis) are unnecessary unless a future feature demands cross-grain composition.
- Should cache entries respect policy effective dates or rely solely on version ids? → Repository lookups should filter for `effective_at <= now` when serving the active policy. Cache keys remain `(policy_key, policy_version)`, but grains can also retain a scheduled “next” version to swap in once it becomes effective; no additional cache invalidation is required beyond reloading on grain activation or explicit publish events.

## Task 5.1 Breakdown — Policy Retrieval Stack

| Step | Status | Layer / Artifact | Responsibility & Notes |
| --- | --- | --- | --- |
| 1 | [x] | **DTOs** | Create/update DTOs under `src/PlayerEngagement.Infrastructure/Persistence/DTOs/XpPolicyDTOs/`: `ActivePolicyDTO`, `PolicyVersionDTO`, `PolicyStreakCurveEntryDTO`, `PolicySeasonalBoostDTO`, `PolicySegmentOverrideDTO`. Mirror the schema defined in `data_model.md`; follow the style seen in [`Identity.Infrastructure/Persistence/DTOs/UserDTO.cs`](https://github.com/aaronlovi/Identity/blob/master/src/Identity.Infrastructure/Persistence/DTOs/UserDTO.cs). |
| 2 | [x] | **Statements** | Add statement classes under `src/PlayerEngagement.Infrastructure/Persistence/Statements/`: `GetActivePolicyStmt`, `GetPolicyVersionStmt`, `ListPublishedPoliciesStmt`, `GetPolicyStreakCurveStmt`, `GetPolicySeasonalBoostsStmt`, `GetPolicySegmentOverridesStmt`. Modeled after [`Identity` statements](https://github.com/aaronlovi/Identity/tree/master/src/Identity.Infrastructure/Persistence/Statements`), each binds parameters, executes via `PostgresExecutor`, and materializes DTOs. |
| 3 | [x] | **DBM service contract & impls** | Update `IPlayerEngagementDbmService` to expose `Task<Result<TDto>>` / `Task<Result<List<TDto>>>` for each query. Implement the methods in `PlayerEngagementDbmService` (Postgres) and `PlayerEngagementDbmInMemoryService`, ensuring the in-memory backing store can serve the same DTO shapes. |
| 4 | [x] | **Mappers** | Add mapper classes in `src/PlayerEngagement.Infrastructure/Policies/Mappers/` that translate DTOs into domain types from `src/PlayerEngagement.Domain/Policies/` (e.g., `PolicyVersionMapper`, `PolicyDocumentMapper`, `PolicySegmentOverrideMapper`). Keep namespaces aligned with folders per `AGENTS.md`. |
| 5 | [ ] | **Slim persistence service** | Implement a thin service in `src/PlayerEngagement.Infrastructure/Policies/Services/` (e.g., `PolicyDocumentPersistenceService`) that depends only on `IPlayerEngagementDbmService` + mappers and returns domain `PolicyDocument` as well as segment override dictionaries. This is what Orleans grains (and later the caching repository) will call. |
| 6 | [ ] | **Repository hand-off** | Once the slim service exists, refactor `PolicyDocumentRepository` so it handles caching only and delegates data retrieval/mapping to the service. |
| 7 | [ ] | **Testing** | For each layer: (a) statement-level tests (parameter binding/result parsing), (b) mapper tests (DTO → domain), (c) DBM service tests (happy path + error handling), (d) slim service tests (aggregation). This ties into the “Write unit tests …” checklist item above. |
