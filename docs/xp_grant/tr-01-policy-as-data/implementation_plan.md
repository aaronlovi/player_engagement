# TR-01 — Policy-as-Data Implementation Plan

## Project Overview (for agents)
This implementation plan captures the work required to satisfy `TR-01 Policy-as-Data` from the Daily Login Bonus XP Grant technical requirements. Before executing tasks, review the supporting context in `AGENTS.md`, `docs/game-engagement-mechanics.md`, `docs/daily_login_bonus.md`, and the XP Grant business, glossary, high-level design, and technical requirements documents under `docs/xp_grant/`. These references define the vocabulary, business goals, system constraints, and architecture assumptions that inform every step below.

## Initiator Brief (for operator)
Completing this plan delivers a versioned, policy-driven configuration system for daily login XP grants. Live-ops operators will author and manage the policies through back-office tools, while backend services consume the same data to calculate streak-based rewards consistently and audibly across segments. Implementation is scoped to the local development environment using Docker Desktop and the compose files in `infra/`; cloud or Kubernetes deployment is deferred.

| Status | Step | Focus | Key Tasks & Artifacts | Notes / Dependencies |
| --- | --- | --- | --- | --- |
| [x] | 1 | Current-State Discovery | Inventory how daily login policies are currently sourced (hard-coded configs, env vars, etc.), map touchpoints in `PlayerEngagement.Infrastructure` and claim orchestrator. | Confirm there is no existing policy document store; capture gaps against TR-01 acceptance criteria. |
| [x] | 2 | Domain Definition | Draft versioned policy domain model covering base XP, streak curves, grace, claim window, streak model, seasonal boosts, segment assignments. | Align vocabulary with glossary terms (policy version, anchor timezone, streak model). Socialize with product for approval. |
| [x] | 3 | Data Modeling | Design persistence schema (tables or document store) for immutable policy versions plus segment mappings; produce ERD and migration plan. | Include audit fields, effective/expiration timestamps, and policy JSON payload if needed. Review against BR-09, BR-13. |
| [ ] | 4 | Database Migration | Implement migrations under `src/PlayerEngagement.Infrastructure/Persistence/Migrations` to create policy tables (policies, policy_versions, segment_policy_map). | Perform `dotnet build`; plan migration rollout with DBAs; ensure non-destructive deployment strategy. |
| [ ] | 5 | Repository & Cache Layer | Add infrastructure code to load latest policy by id/version, cache results, and expose interfaces to domain services. | Follow AGENTS coding guidelines; unit-test repository behavior (version fetch, history read). |
| [ ] | 6 | Policy CRUD APIs | Implement Policy Service endpoints for create, read (by id & version), list history, and retire policies; enforce immutability post-publication. | Generate OpenAPI/Protobuf; include validation (streak model enum, claim window bounds). |
| [ ] | 7 | Segment Resolution Integration | Update claim orchestration to resolve player segment → policy id, retrieve specific version, and include `policy_version` on every claim record. | Validate idempotency path (TR-04) still holds; add functional tests around multi-segment scenarios. |
| [ ] | 8 | Admin Console Updates | Extend Angular admin UI (`ui/player-engagement-config-ui`) to manage policy lifecycle (forms, previews, publish flow) leveraging new endpoints. | Provide UX for previewing streak curve outputs; ensure access controls. |
| [ ] | 9 | Audit & Analytics | Emit policy change events to telemetry bus and persist operator metadata for audits; ensure claims log policy version for analytics queries. | Coordinate with observability team; document metrics and dashboards. |
| [ ] | 10 | Testing Strategy | Build automated test suite: domain unit tests (policy serialization, streak model validation), integration tests for CRUD + claim flow, end-to-end happy path via test harness. | Run `dotnet test` and front-end tests (`npm test`) per AGENTS checklist; capture coverage deltas. |
| [ ] | 11 | Documentation & Runbooks | Update `AGENTS.md`, admin UI README, and operator docs to describe policy management workflow, rollback steps, and guardrails. | Include change management notes for live-ops; link to workflow checklist expectations. |
| [ ] | 12 | Rollout & Monitoring | Stage migration in lower env, seed initial policy versions, validate claims referencing version ids; prepare rollback plan and production deployment checklist. | Monitor post-release metrics (claim success rate, policy lookup latency); schedule post-launch review. |

## Step Breakdown Documents
- Step 1: docs/xp_grant/tr-01-policy-as-data/step-01-current-state-discovery.md
- Step 2: docs/xp_grant/tr-01-policy-as-data/step-02-domain-definition.md
- Step 3: docs/xp_grant/tr-01-policy-as-data/step-03-data-modeling.md
- Step 4: docs/xp_grant/tr-01-policy-as-data/step-04-database-migration.md
- Step 5: docs/xp_grant/tr-01-policy-as-data/step-05-repository-cache-layer.md
- Step 6: docs/xp_grant/tr-01-policy-as-data/step-06-policy-crud-apis.md
- Step 7: docs/xp_grant/tr-01-policy-as-data/step-07-segment-resolution-integration.md
- Step 8: docs/xp_grant/tr-01-policy-as-data/step-08-admin-console-updates.md
- Step 9: docs/xp_grant/tr-01-policy-as-data/step-09-audit-analytics.md
- Step 10: docs/xp_grant/tr-01-policy-as-data/step-10-testing-strategy.md
- Step 11: docs/xp_grant/tr-01-policy-as-data/step-11-documentation-runbooks.md
- Step 12: docs/xp_grant/tr-01-policy-as-data/step-12-rollout-monitoring.md
