# Step 5 — Repository & Cache Layer

## Objective
Provide infrastructure services that load policy documents efficiently and expose them to domain consumers with caching and version awareness.

## Inputs
- Updated schema and entities from Steps 3–4.
- Existing repository patterns in `PlayerEngagement.Infrastructure`.
- Cache strategy guidelines (in-memory, distributed) used elsewhere in the project.

## Tasks
- [ ] Implement repositories or data access objects for policy retrieval by id, by version, and by segment assignment.
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
- Do we require distributed cache coherence across Orleans silos?
- Should cache entries respect policy effective dates or rely solely on version ids?
