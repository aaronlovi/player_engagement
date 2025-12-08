# SeasonGrain Implementation Notes

## Work done

- Added a season calendar DTO stub (`SeasonCalendarDTO`) and DbmService interface stubs (`GetCurrentSeasonAsync`) with placeholder implementations (returns empty) in DbmService and InMemory Dbm.
- Introduced `ISeasonBoundaryProvider` interface and a `SeasonBoundaryProvider` implementation that loads season data via DbmService, blocks until initial load, caches boundaries, and exposes `GetCurrentSeasonAsync`/`RefreshAsync`.
- No schema migration or Orleans grain added yet; provider is a placeholder until a season calendar table exists and wiring is defined.

## Next steps

- Define/implement the season calendar schema and Dbm queries to return current/next season data.
- Decide API shape for “no active season” (null vs. include next-start info) before wiring callers.
- Wire season provider (or future SeasonGrain) into UserGrain/claim orchestration when those surfaces exist; add metrics/telemetry later.
- Add unit tests when a grain/provider test harness is available; owner to run `dotnet test src/PlayerEngagement.sln` after wiring.

## Metadata

### Status

partial

### Confidence

Medium — interfaces and stubs exist; real data path and wiring still pending.

### Dependencies

- AGENTS.md
- .prompts/season-grain-plan/plan.md
- IPlayerEngagementDbmService and SeasonBoundaryInfo usage in streak engine

### Open Questions

- Season data schema specifics and admin workflow; explicit handling for “no active season” responses.

### Assumptions

- Single-instance app; no multi-node/keep-alive concerns yet; Dbm will later provide season calendar data.
