# Task 3 – V001__init_schema.sql Planning Checklist

Goal: author the first XP migration (`V001__init_schema.sql`) using the same conventions as `Identity.Infrastructure/Persistence/Migrations` so the future `Xp.Infrastructure` layer plugs into `InnoAndLogic.Persistence` cleanly.

## Step-by-Step Plan

1. **Collect Requirements**
   - Revisit `docs/xp_grant/xp_grant_high_level_design.md` (storage section) and `docs/xp_grant/xp_grant_technical_requirements.md` (TR-01, TR-02, TR-04, TR-06, TR-07).
   - Extract mandatory entities, JSON payloads, uniqueness rules, and audit requirements.

2. **Align With Identity Infrastructure Pattern**
   - Mirror the directory layout: `src/Xp.Infrastructure/Persistence/Migrations`.
   - Use the `${schema}` token and lower-case file naming (`V001__init_schema.sql`) exactly as `Identity` does so the `DbMigrations` loader can perform token substitution.
   - Ensure accompanying folders (`Statements`, DTOs, Dbm services) can be added later without relocating the migration.

3. **Establish Schema Bootstrap**
   - Begin the script with `create schema if not exists ${schema};` and helper functions (e.g., `${schema}.now_utc()`), following the Identity baseline style.
   - Set `search_path` via `set local search_path to ${schema};` (or explicit schema-qualification) to keep statements consistent.

4. **Draft Table Specifications**
   - For each required table (`xp_users`, `xp_ledger`, `xp_balance`, `xp_streaks`, `xp_rules`, `xp_awards`), write a spec that covers:
     - Column names and types (prefer `BIGINT`, `NUMERIC`, `JSONB`, `TIMESTAMPTZ` where appropriate).
     - Defaults (e.g., `now_utc()`, zero balances), nullable vs required fields.
     - Check constraints (positive XP amounts, non-negative streak counters).

5. **Design Keys, Indexes & Relationships**
   - Choose primary keys (surrogate identity columns for user rows, composite keys for ledger/awards when needed).
   - Add foreign keys back to `${schema}.xp_users`.
   - Define unique indexes for idempotency (`xp_awards` on `(user_id, reward_day_id)`, `xp_ledger` on `correlation_id`).
   - Create helpful secondary indexes (e.g., `created_at`, `policy_version`, `season_id`) mirroring the Identity pattern of indexing read paths.

6. **Author the Migration Script**
   - Write the final SQL under `src/Xp.Infrastructure/Persistence/Migrations/V001__init_schema.sql`.
   - Group related DDL blocks with `-------------------------------------------------------------------------------` separators and `COMMENT ON` statements, matching the reference repo’s style.
   - Keep statements schema-qualified (`${schema}.table`) to avoid relying on session state.

7. **Validate Against Local Postgres**
   - Use the compose database (`docker compose -f infra/docker-compose.yml up -d`) if it isn’t already running.
   - Apply the migration manually via `psql` (substituting `${schema}` with `xp`) or by wiring the future `DbmService` harness.
   - Inspect tables (`\dt xp.*`), columns (`\d xp.xp_ledger`), and constraints to confirm alignment with the spec.

8. **Document Follow-ups**
   - Note any open questions (e.g., additional helper functions, sequences) for subsequent tasks.
   - Update `docs/scaffolding/scaffolding_detailed_tasks.md` Task 3 notes to reflect the chosen structure and identity alignment.

## Deliverables

- File: `src/Xp.Infrastructure/Persistence/Migrations/V001__init_schema.sql`.
- Verification notes or command transcript confirming the migration applies cleanly to Postgres.

---

## Step 1 Findings – Schema Requirements Snapshot

| Entity / Table | Required Fields & Notes | Constraints & Indexes | Primary Sources |
| -------------- | ---------------------- | ---------------------- | ---------------- |
| `xp_users` | `user_id` (bigint identity); `reward_tz` (IANA string); optional segment or cohort identifiers; created/updated timestamps. Also needs history hook for tz changes. | PK on `user_id`; index on `reward_tz`; future FK targets from streaks/balance/ledger. | HLD §7 (`users` row), TR-11 (timezone governance), TR-10 (segmentation). |
| `xp_ledger` | `ledger_id` (bigint identity); `user_id`; `amount` (numeric); `reason` enum/text (`DAILY_LOGIN` baseline); `correlation_id` (idempotency key / receipt); `policy_version`; `season_id`; timestamps; optional context JSON. | PK on `ledger_id`; unique constraint on `correlation_id`; FK to `xp_users`; index on (`user_id`, `created_at`); maybe index on `policy_version`. Append-only. | HLD data contracts, TR-07 (ledger), TR-04 (idempotent claim), TR-08 (concurrency). |
| `xp_balance` | `user_id`; `current_balance` (numeric); `lifetime_xp`; `seasonal_xp`; `updated_at`. Serves as quick lookup. | PK/FK on `user_id` referencing `xp_users`; updated via trigger (Task 4). | Task list & scaffolding requirements, inferred from BR-11 (progression integration). |
| `xp_streaks` (`streak_state`) | `user_id`; `current_streak`; `last_reward_day_id`; `grace_used`; `longest_streak`; `model_state` (JSONB for plug-ins). | PK/FK on `user_id`; check constraints for non-negative counters; may need index on `last_reward_day_id`. | HLD data contracts, TR-02, TR-06. |
| `xp_rules` / `policy` | `policy_id` (text key); `version` (int); `is_active` flag; `definition` JSONB containing base XP, curves, windows, grace, seasonal overrides, segments; `created_at`; `created_by`. | Composite PK on (`policy_id`, `version`); unique active pointer; index on `is_active`; immutable rows beyond metadata. | TR-01 (policy-as-data), HLD data contracts, Business BR-13/BR-09. |
| `xp_awards` (`daily_login_award`) | `award_id` (bigint identity); `user_id`; `reward_day_id` (date + tz key); `xp_awarded`; `streak_day`; `model_state_snapshot`; `policy_version`; `receipt_id`; `created_at`. | PK on `award_id`; unique constraint on (`user_id`, `reward_day_id`); FK to `xp_users`; index on `policy_version` for analytics. | HLD storage table + TR-04 (idempotent claim). |
| Helper objects | Schema `xp`; helper function `${schema}.now_utc()` for timestamp defaults; sequences handled via `generated always as identity`. | Ensure search path or schema-qualified names; align with Identity pattern. | Identity repo baseline migration; plan Step 3. |

Additional considerations captured for later steps:
- All monetary/XP amounts should use `NUMERIC(18,2)` or `BIGINT` (define in Step 4) while ensuring non-negative via `CHECK`.
- Timestamps should default to `now_utc()` to avoid timezone skew.
- JSONB fields (`model_state`, `definition`) should default to `'{}'::jsonb` and enforce `NOT NULL`.
- Future triggers (Task 4) will depend on `xp_ledger` inserts to update `xp_balance`; ensure ledger columns contain enough data (`amount`, `is_credit`) for that trigger.

These notes will drive the column definitions in Step 4.

---

## Step 2 Notes – Identity Infrastructure Alignment

- Created the XP infrastructure scaffolding directories to mirror the Identity layout:
  - `src/Xp.Infrastructure/Persistence/Migrations`
  - `src/Xp.Infrastructure/Persistence/Statements`
  - `src/Xp.Infrastructure/Persistence/DTOs`
- Confirmed future migration files will be named `V###__description.sql` (e.g., `V001__init_schema.sql`) and leverage the `${schema}` token for substitution by `DbMigrations`.
- We will follow the same separator/comment convention (`-------------------------------------------------------------------------------`) and schema-qualified statements (`${schema}.table`) used in `Identity.Infrastructure`.
- Statements and DTO folders remain empty for now but establish the path for upcoming repository and query objects.
