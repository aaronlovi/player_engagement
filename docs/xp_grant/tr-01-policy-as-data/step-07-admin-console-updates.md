# Step 8 — Admin Console Updates

## Objective

Enhance the Angular admin interface so live-ops teams can manage policy versions end-to-end within `ui/player-engagement-config-ui`.

## Inputs

- Policy API contracts from Step 6.
- Current admin UI features documented in `ui/player-engagement-config-ui/README.md`.
- Design requirements from business docs (BR-13, BR-14).

## Tasks

| # | Done | Task |
| --- | --- | --- |
| 1 | [x] | Admin API client: add service methods for list/create/publish/retire/update-overrides; wire auth headers/error handling. |
| 2 | [x] | Policy list view: render paged list with status/effective dates; link to detail/edit routes. |
| 3 | [x] | Policy create/edit shell: scaffold form layout with base fields (display name, XP, currency, claim window, anchor strategy, grace). |
| 4 | [x] | Streak curve editor component: grid for day/multiplier/bonus/cap; add row/reorder; client-side validation per rules. |
| 5 | [x] | Seasonal boosts editor: list/add/edit boost rows with date pickers and overlap validation. |
| 6 | [x] | Preview settings + streak model parameters UI: inputs per model type, toggle default segment, sample window. |
| 7 | [x] | Validation/feedback: inline field errors, submit blocking, success/error toasts; display API validation messages. |
| 8 | [x] | Publish/retire actions: buttons with confirm dialogs; call publish/retire endpoints and refresh state. |
| 9 | [ ] | History/diff view: show version list for a policy and render side-by-side diff of selected versions. |
| 10 | [ ] | Segment overrides UI: table/form to map segment → policy version; uses overrides endpoint. |
| 11 | [ ] | Auth/guards: route guard and action-level checks for operator roles; hide disabled actions. |
| 12 | [ ] | Testing: unit tests for services/components; e2e happy paths (create→publish, retire, edit overrides). |

## Deliverables

- Updated Angular components/services with associated tests.
- Documentation snippet (in README or dedicated doc) explaining operator workflow.

## References

- `AGENTS.md` notes on front-end placement within project structure.
- `docs/daily_login_bonus.md` for context when designing preview messaging.

## Open Questions

- Do operators require draft collaboration features (e.g., comments, staged approvals)?
- Should the UI visualize projected XP payouts for different streak models via charts?

## Streak Model Reference (for UI wiring)

| Model | Key Parameters | Notes |
| --- | --- | --- |
| PlateauCap | `plateauDay:int`, `plateauMultiplier:decimal` | Multiplier caps after plateauDay. |
| WeeklyCycleReset | *(none)* | Fixed 7-day cycle. |
| DecayCurve | `decayPercent:decimal`, `graceDay:int` | Decay applied after graceDay. |
| TieredSeasonalReset | `tiers:[{ startDay:int, endDay:int, bonusMultiplier:decimal }]` | Non-overlapping tiers. |
| MilestoneMetaReward | `milestones:[{ day:int, rewardType:string, rewardValue:string }]` | Milestone-based rewards. |
