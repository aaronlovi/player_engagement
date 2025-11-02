### Daily Login Bonus – XP Grant

**Section:** High‑Level Design (HLD)

**Scope:** Components, data contracts, flows, and cross‑cutting concerns to fulfill Business & Technical Requirements.

**Assumptions:** Relational DB (Postgres/MariaDB), backend in Elixir or C#/Orleans, k8s on AWS/GCP, Prometheus/Grafana, IANA timezones, single primary region initially.

---

#### 1) Component Inventory

| **Component**                            | **Responsibility**                                                    | **Key Inputs**                                            | **Key Outputs**                                 | **Notes**                                                                 | **Linked Requirements**                                       |
| ---------------------------------------- | --------------------------------------------------------------------- | --------------------------------------------------------- | ----------------------------------------------- | ------------------------------------------------------------------------- | ------------------------------------------------------------- |
| API Gateway / Web Edge                   | AuthN/AuthZ, rate limits, routing to Services.                        | Auth token, client metadata.                              | Routed request, 429/401s.                       | Could be NGINX/Envoy/API GW; per‑IP/user rate limits.                     | BR-08; TR-09, TR-17, TR-20, TR-24                             |
| Daily Login Service (Claim Orchestrator) | Idempotent claim endpoint; computes reward‑day; coordinates XP grant. | User id, policy id, current time (UTC), `user.reward_tz`. | Claim receipt, XP awarded, streak snapshot.     | Stateless; uses DB uniqueness for correctness.                            | BR-01, BR-04, BR-12; TR-03, TR-04, TR-08, TR-09, TR-23        |
| Streak Engine                            | Pure functions for streak transitions across models.                  | Policy doc, last streak state, today’s reward‑day.        | New streak state, XP multiplier/bonus.          | Model plug‑ins: Plateau, Weekly Reset, Decay, Seasonal Reset, Milestones. | BR-02, BR-16; TR-02, TR-06, TR-26                             |
| Policy Service                           | CRUD/versioned policies; segment mapping.                             | Operator inputs, segment resolver.                        | Policy documents, `policy_version`.             | Cached aggressively; audit history immutable.                             | BR-13, BR-09, BR-07, BR-06; TR-01, TR-10, TR-12, TR-22, TR-29 |
| XP/Progression Service                   | Ledger append, level calc, event emission.                            | XP amount, reason, correlation id.                        | Updated totals/level, `XPLedgerAppended` event. | Single source of truth for XP.                                            | BR-11, BR-10; TR-07, TR-09, TR-15, TR-28                      |
| User Profile Service                     | Stores `reward_tz`, tz change cooldown & history.                     | Tz change requests, device/geo hints.                     | Effective tz, next‑boundary switchover.         | Enforces cooldown + effective‑next‑boundary rule.                         | BR-04; TR-11, TR-24                                           |
| Events & Telemetry Bus                   | Async analytics & audit fan‑out.                                      | Domain events.                                            | Timeseries, logs, audit rows.                   | Kafka/PubSub/SQS+Firehose equivalent.                                     | BR-05; TR-14, TR-15, TR-28                                    |
| Admin Console                            | Operator UI for policies, previews, support tools.                    | Policy edits, user lookup.                                | Published policies, dry‑run results.            | Reads from Policy/Claim preview endpoints.                                | BR-13, BR-14; TR-01, TR-13, TR-25                             |

---

#### 2) Core Data Contracts (Logical)

| **Entity / Event**        | **Fields (logical)**                                                                                                  | **Purpose**                                  | **Linked Requirements**                                |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------- | -------------------------------------------- | ------------------------------------------------------ |
| `daily_login_award`       | user_id, reward_day_id (YYYY‑MM‑DD in reward timezone), xp_awarded, streak_day, model_state, policy_version, receipt_id, created_at | Idempotent record of a day’s claim per user. | BR-01, BR-04, BR-12, BR-09; TR-04, TR-08, TR-07        |
| `streak_state`            | user_id, current_streak, last_reward_day_id, grace_used, longest_streak, model_state                                  | Quick path to compute next outcome.          | BR-02, BR-16, BR-03; TR-06, TR-26                      |
| `xp_ledger`               | user_id, amount, reason=DAILY_LOGIN, correlation_id=receipt_id, policy_version, season_id, created_at                 | Append‑only XP journal.                      | BR-11, BR-10; TR-07, TR-28, TR-15                      |
| `policy`                  | policy_id, version, base_xp, streak_curve, model, claim_window, grace, seasonal, segment rules                        | Policy‑as‑data; immutable versions.          | BR-13, BR-09, BR-07, BR-06, BR-16; TR-01, TR-02, TR-12 |
| Event `DailyLoginClaimed` | user_id, reward_day_id (YYYY‑MM‑DD in reward timezone), xp, streak_data, policy_version, receipt_id                    | Downstream analytics and triggers.           | BR-05; TR-14, TR-15, TR-13                             |

---

#### 3) Reward‑Day & Timezone Resolution

| **Step**               | **Logic**                                                             | **Outcome**                          | **Linked Requirements**           |
| ---------------------- | --------------------------------------------------------------------- | ------------------------------------ | --------------------------------- |
| Anchor TZ              | Read `user.reward_tz` (IANA) from profile; do not trust device clock. | Stable per‑user reference.           | BR-04; TR-03, TR-11               |
| Claim Window           | Use policy start (e.g., 05:00 local) to build daily boundary.         | Avoids midnight edge cases/DST pain. | BR-01, BR-04; TR-03               |
| Calculate Day          | Convert `now_utc` → zoned; if `< boundary` use yesterday else today.  | Deterministic `reward_day_id`.       | BR-04; TR-03, TR-26               |
| Anti‑double claim      | Natural key `(user_id, reward_day_id)` with unique index.             | One claim per user per logical day.  | BR-01, BR-04, BR-12; TR-04, TR-08 |
| Rolling Cooldown (opt) | Deny if last claim < N hours ago; return prior receipt.               | Extra guard vs extreme travel.       | BR-04, BR-03; TR-05               |

---

#### 4) Streak Models (Plug‑in Strategy)

| **Model**              | **Rule Summary**                                          | **Stored State**                  | **Notes**                     | **Linked Requirements**    |
| ---------------------- | --------------------------------------------------------- | --------------------------------- | ----------------------------- | -------------------------- |
| Plateau/Cap            | Escalate to cap day (e.g., 7), then hold.                 | `current_streak`, `cap_day`       | Simple & durable.             | BR-16, BR-02; TR-02, TR-06 |
| Weekly Cycle Reset     | Day 1..7 then reset to 1.                                 | `current_streak (1..7)`           | Resets each 7‑day cycle.      | BR-16, BR-02; TR-02, TR-06 |
| Decay/Soft Reset       | After cap or N days, reduce to baseline or mid‑tier.      | `current_streak`, `decay_counter` | Requires clear UI copy.       | BR-16, BR-02; TR-02, TR-06 |
| Tiered Seasonal Reset  | Streak persists within season; resets at season boundary. | `current_streak`, `season_id`     | Season dates from policy.     | BR-16, BR-06; TR-02, TR-12 |
| Milestone Meta‑Rewards | XP plateaus; milestones unlock cosmetics/titles.          | `milestone_flags`                 | Emits separate unlock events. | BR-16; TR-02               |

---

#### 5) Key Flows (Happy Path)

| **Flow**       | **Steps (summary)**                                                                                                                                                               | **Idempotency/Guards**                                               | **Outputs**                                              | **Linked Requirements**                                                                   |
| -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- | -------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Claim          | Auth → Resolve reward‑day → Load policy/segment → Streak Engine compute → Insert `daily_login_award` (unique key) → Append `xp_ledger` → Emit event → Return receipt & UI deltas. | DB unique index; optional cooldown; retries return existing receipt. | `receipt_id`, xp amount, streak counters, next boundary. | BR-01, BR-02, BR-04, BR-12, BR-11, BR-10; TR-03, TR-04, TR-07, TR-08, TR-09, TR-14, TR-15 |
| Eligibility    | Auth → Resolve reward‑day → Dry‑run Streak Engine (no writes).                                                                                                                    | N/A                                                                  | Eligibility flag, projected XP, next boundary.           | BR-14; TR-09, TR-13                                                                       |
| TZ Change      | Validate cooldown → Schedule effective at next boundary → Audit log.                                                                                                              | Cooldown & rate limit.                                               | New tz effective timestamp; no duplicate day ids.        | BR-04; TR-11, TR-17                                                                       |
| Policy Publish | Validate JSON schema → Version bump → Invalidate caches → Audit.                                                                                                                  | Immutable versions.                                                  | New `policy_version` live within 60s.                    | BR-09, BR-13; TR-01, TR-22, TR-29, TR-25                                                  |

---

#### 6) Public API Surface (Logical)

| **Endpoint / Method**          | **Request (essential)**                 | **Response (essential)**                                                 | **Notes**                  | **Linked Requirements**                         |
| ------------------------------ | --------------------------------------- | ------------------------------------------------------------------------ | -------------------------- | ----------------------------------------------- |
| `GET /daily-login/eligibility` | auth, user_id                           | eligible, projected_xp, streak_preview, next_boundary_ts, policy_version | Safe precheck.             | BR-14; TR-09, TR-13, TR-27                      |
| `POST /daily-login/claim`      | auth, user_id, optional idempotency key | status {NEW/ALREADY_CLAIMED}, receipt_id, xp_awarded, streak             | Idempotent via DB unique.  | BR-01, BR-12, BR-04; TR-03, TR-04, TR-09, TR-23 |
| `POST /users/{id}/reward-tz`   | tz (IANA)                               | effective_from_boundary_ts                                               | Enforces cooldown + audit. | BR-04; TR-11, TR-24                             |
| `GET/POST /policies`           | policy docs                             | versions, publish status                                                 | Admin only.                | BR-13, BR-09; TR-01, TR-29, TR-25               |

---

#### 7) Storage (Relational Logical Schema)

| **Table**           | **Key(s)**                             | **Indexes**                          | **Notes**                                      | **Linked Requirements**           |
| ------------------- | -------------------------------------- | ------------------------------------ | ---------------------------------------------- | --------------------------------- |
| `daily_login_award` | PK(id), **UK(user_id, reward_day_id)** | idx(policy_version), idx(created_at) | Core idempotency surface.                      | BR-01, BR-04, BR-12; TR-04, TR-08 |
| `streak_state`      | PK(user_id)                            | none (row‑per‑user)                  | Cached; recomputable from awards.              | BR-02, BR-16, BR-03; TR-06        |
| `xp_ledger`         | PK(id), FK(user_id)                    | idx(user_id, created_at)             | Append‑only; no updates.                       | BR-11, BR-10; TR-07               |
| `policies`          | PK(policy_id, version)                 | idx(active_flag)                     | Immutable versions; soft‑activate via pointer. | BR-13, BR-09, BR-16; TR-01, TR-02 |
| `users` (profile)   | PK(user_id)                            | idx(reward_tz)                       | Tz history in side table with cooldowns.       | BR-04; TR-11                      |

---

#### 8) Deployment & Scaling

| **Area**     | **HLD Decision**                                              | **Why**                        |
| ------------ | ------------------------------------------------------------- | ------------------------------ |
| API/Services | Stateless pods behind HPA; keep per‑request work small.       | Horizontal elasticity.         |
| DB           | Single primary + read replicas; pgbouncer/connection pooling. | Protect DB; smooth spikes.     |
| Caching      | In‑memory/redis for policies; TTL 5–15m; bust on publish.     | Low latency; simple coherence. |
| DR           | Backups + point‑in‑time recovery; infra as code.              | Safety net.                    |

---

#### 9) Observability & Ops

| **Aspect**          | **What We Capture**                                                                                                                     | **Use**                          |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------- |
| Metrics             | claims_total, already_claimed_total, xp_granted_sum, streak_length_hist, grace_usage_total, p95_latency, error_rate, by policy/segment. | SLOs, tuning, anomaly detection. |
| Logs                | JSON with `receipt_id`, `policy_version`, user_id (hashed), reward_day_id, model decisions.                                             | Debug/audit.                     |
| Tracing             | Spans: eligibility, claim orchestration, DB write, ledger append.                                                                       | Slow‑path diagnosis.             |
| Dashboards & Alerts | SLO, RED metrics, duplicate attempts spike, DB errors, cache hit rate.                                                                  | On‑call readiness.               |

---

#### 10) Security & Abuse Controls

| **Control**       | **Mechanism**                                           | **Notes**                    |
| ----------------- | ------------------------------------------------------- | ---------------------------- |
| AuthN/Z           | Platform tokens; server verifies user owns the claim.   | Least‑priv DB roles.         |
| Rate Limits       | Per‑IP & per‑user at edge; captcha on anomalies.        | Configurable per env.        |
| TZ Governance     | Cooldown + max changes/season; effective‑next‑boundary. | Prevents tz hopping exploit. |
| Data Minimization | Store only necessary PII; hash user ids in logs.        | Compliance friendly.         |

---

#### 11) Failure Modes & Recovery

| **Failure**             | **Behavior**                               | **Operator Action**      |
| ----------------------- | ------------------------------------------ | ------------------------ |
| Double submit / retries | "ALREADY_CLAIMED" with same receipt.       | None; expected.          |
| DB unique violation     | Return existing record.                    | Monitor metric spike.    |
| Cache outage            | Fall back to DB; higher latency only.      | Warm cache; review TTLs. |
| Clock skew (rare)       | Server clock authoritative; NTP monitored. | Alarm on drift >100ms.   |

---

#### 12) Open Questions (for LLD)

| **Topic**    | **Question**                                          | **Decision Owner** |
| ------------ | ----------------------------------------------------- | ------------------ |
| Decay curves | Exact parameterization (percent/interval)?            | Product/Live‑ops   |
| Milestones   | Cosmetic inventory + delivery pipeline?               | Art/Live‑ops       |
| API          | gRPC vs REST first?                                   | Eng                |
| Multi‑region | Read‑write topology & day id guarantees cross‑region? | Platform Eng       |
