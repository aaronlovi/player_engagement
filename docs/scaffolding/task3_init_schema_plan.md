> Follow-up items for this task now live in `docs/scaffolding/scaffolding_followups.md`. Keep this checklist only while Task 3 remains in flight; it can be archived once the migration is finalized.

# Task 3 – V001__init_schema.sql Planning Checklist

Goal: author the first XP migration (`V001__init_schema.sql`) using the same conventions as `Identity.Infrastructure/Persistence/Migrations` so the future `PlayerEngagement.Infrastructure` layer plugs into `InnoAndLogic.Persistence` cleanly.

## Step-by-Step Plan

1. **Collect Requirements**
   - Revisit `docs/xp_grant/xp_grant_high_level_design.md` (storage section) and `docs/xp_grant/xp_grant_technical_requirements.md` (TR-01, TR-02, TR-04, TR-06, TR-07).
   - Extract mandatory entities, JSON payloads, uniqueness rules, and audit requirements.

2. **Align With Identity Infrastructure Pattern**
   - Mirror the directory layout: `src/PlayerEngagement.Infrastructure/Persistence/Migrations`.
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
   - Write the final SQL under `src/PlayerEngagement.Infrastructure/Persistence/Migrations/V001__init_schema.sql`.
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

- File: `src/PlayerEngagement.Infrastructure/Persistence/Migrations/V001__init_schema.sql`.
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
  - `src/PlayerEngagement.Infrastructure/Persistence/Migrations`
  - `src/PlayerEngagement.Infrastructure/Persistence/Statements`
  - `src/PlayerEngagement.Infrastructure/Persistence/DTOs`
- Confirmed future migration files will be named `V###__description.sql` (e.g., `V001__init_schema.sql`) and leverage the `${schema}` token for substitution by `DbMigrations`.
- We will follow the same separator/comment convention (`-------------------------------------------------------------------------------`) and schema-qualified statements (`${schema}.table`) used in `Identity.Infrastructure`.
- Statements and DTO folders remain empty for now but establish the path for upcoming repository and query objects.

---

## Step 3 – Schema Bootstrap Outline

- Open the migration with Identity-style boilerplate:
  - `create schema if not exists ${schema};`
  - `set local search_path to ${schema}, public;`
- Define a reusable UTC helper:
  ```sql
  create or replace function ${schema}.now_utc() returns timestamp with time zone as $$
      select now() at time zone 'utc';
  $$ language sql stable;
  ```
- All timestamp columns (`created_at`, `updated_at`, etc.) will default to `${schema}.now_utc()`.
- We do **not** need a `generator` table because XP ids will use `generated always as identity`, unlike Identity’s manual id strategy.
- Maintain `-------------------------------------------------------------------------------` separators and add `COMMENT ON` statements for tables/columns where clarity matters.

---

## Step 4 – Table Specification Drafts

**Conventions**
- `bigint` for identifiers and XP totals; `integer` for streak counters.
- Timestamp columns default to `${schema}.now_utc()`.
- JSON payloads default to `'{}'::jsonb` and are declared `not null`.
- Numeric XP values remain non-negative unless a future requirement introduces explicit debits.

**`${schema}.xp_users`**
- Columns: `user_id bigint generated always as identity primary key`, `external_user_id text not null`, `reward_tz text not null`, `segment_key text null`, `created_at timestamptz not null default ${schema}.now_utc()`, `updated_at timestamptz not null default ${schema}.now_utc()`.
- Indexes/Constraints: unique index on `external_user_id`; FK target for all other tables.

**`${schema}.xp_ledger`**
- Columns: `ledger_id bigint generated always as identity primary key`, `user_id bigint not null`, `amount bigint not null`, `reason text not null`, `correlation_id uuid not null`, `policy_key text not null`, `policy_version int not null`, `season_id text null`, `metadata jsonb not null default '{}'::jsonb`, `created_at timestamptz not null default ${schema}.now_utc()`.
- Constraints: FK to `xp_users`; check `amount <> 0`.
- Indexes: unique on `correlation_id`; btree on `(user_id, created_at desc)`; optional composite on `(policy_key, policy_version)`.

**`${schema}.xp_balance`**
- Columns: `user_id bigint primary key`, `current_balance bigint not null default 0`, `lifetime_xp bigint not null default 0`, `seasonal_xp bigint not null default 0`, `season_id text null`, `updated_at timestamptz not null default ${schema}.now_utc()`.
- Constraints: FK to `xp_users`, checks ensuring each XP column ≥ 0.
- Notes: maintained via balance trigger in Task 4.

**`${schema}.xp_streaks`**
- Columns: `user_id bigint primary key`, `current_streak int not null default 0`, `longest_streak int not null default 0`, `grace_used int not null default 0`, `last_reward_day_id text null`, `model_state jsonb not null default '{}'::jsonb`, `updated_at timestamptz not null default ${schema}.now_utc()`.
- Constraints: FK to `xp_users`; checks forcing non-negative counters.
- Indexes: optional index on `last_reward_day_id` for analytics/queries.

**`${schema}.xp_rules`**
- Columns: `policy_key text not null`, `version int not null`, `is_active boolean not null default false`, `definition jsonb not null`, `created_by text not null`, `created_at timestamptz not null default ${schema}.now_utc()`, `updated_at timestamptz not null default ${schema}.now_utc()`.
- Constraints: PK on `(policy_key, version)`; partial unique index enforcing single active version (`where is_active`).
- Notes: row immutability per version; updates happen by inserting a new version.

**`${schema}.xp_awards`**
- Columns: `award_id bigint generated always as identity primary key`, `user_id bigint not null`, `reward_day_id text not null`, `xp_awarded bigint not null`, `streak_day int not null`, `policy_key text not null`, `policy_version int not null`, `receipt_id uuid not null`, `model_state_snapshot jsonb not null default '{}'::jsonb`, `created_at timestamptz not null default ${schema}.now_utc()`.
- Constraints: FK to `xp_users`; unique constraint on `(user_id, reward_day_id)`; checks `xp_awarded > 0` and `streak_day >= 0`.
- Indexes: composite on `(policy_key, policy_version)` for analytics, plus optional `reward_day_id`.

Draft choices above will be validated and tightened in Step 5 when finalizing keys and relationships.

---

## Step 5 – Keys, Relationships & Index Strategy

- **Foreign Keys**
  - `xp_ledger.user_id`, `xp_balance.user_id`, `xp_streaks.user_id`, and `xp_awards.user_id` reference `${schema}.xp_users(user_id)` with `ON DELETE CASCADE` so removing a user cleans dependent projections/awards while ledger rows follow user lifecycle policies.
  - `xp_balance` and `xp_streaks` are 1:1 with `xp_users`; enforce via PK=FK design.
- **Primary Keys**
  - Identity columns (`generated always as identity`) provide surrogate PKs for `xp_users`, `xp_ledger`, and `xp_awards`.
  - Composite primary key for `xp_rules` on `(policy_key, version)`; `xp_balance`/`xp_streaks` use the FK-as-PK pattern.
- **Unique Constraints**
  - `xp_users_external_user_id_uidx` ensures one XP profile per platform user.
  - `xp_ledger_correlation_id_uidx` enforces idempotent claims.
  - `xp_awards_user_day_uidx` prevents duplicate daily awards.
  - Partial unique index `xp_rules_active_uidx` on `(policy_key)` where `is_active` is true guarantees a single active version.
- **Indexes**
  - `xp_ledger_user_created_at_idx` on `(user_id, created_at DESC)` optimizes balance/streak recalculations and history lookups.
  - `xp_ledger_policy_idx` on `(policy_key, policy_version)` supports analytics by rule.
  - `xp_awards_policy_idx` on `(policy_key, policy_version)` mirrors ledger analytics; optional `xp_awards_reward_day_idx` on `reward_day_id` for reporting.
  - `xp_streaks_last_reward_day_idx` (optional) enables troubleshooting around reward day transitions.
  - `xp_users_reward_tz_idx` on `reward_tz` helps find cohorts for timezone governance.
- **Check Constraints**
  - `xp_ledger_amount_chk` (`amount <> 0`).
  - `xp_balance_totals_chk` ensuring `current_balance >= 0`, `lifetime_xp >= 0`, `seasonal_xp >= 0`.
  - `xp_streaks_non_negative_chk` covering `current_streak`, `longest_streak`, `grace_used`.
  - `xp_awards_positive_chk` (`xp_awarded > 0`, `streak_day >= 0`).
- **Comments**
  - Add `COMMENT ON` statements for each table/critical column (e.g., correlation id, policy version) mirroring Identity’s style for clarity.

These decisions lock the relational model and will be carried directly into the migration in Step 6.

---

## Step 6 – Migration Authoring Checklist

1. Create `src/PlayerEngagement.Infrastructure/Persistence/Migrations/V001__init_schema.sql`.
2. Add banner line (`-------------------------------------------------------------------------------`) separators between major sections, following the Identity baseline.
3. Bootstrap:
   - `create schema if not exists ${schema};`
   - `set local search_path to ${schema}, public;`
   - `create or replace function ${schema}.now_utc() ...`
4. `CREATE TABLE` blocks in order:
   - `xp_users`
   - `xp_ledger`
   - `xp_balance`
   - `xp_streaks`
   - `xp_rules`
   - `xp_awards`
5. For each table:
   - Define columns per Step 4.
   - Apply constraints, foreign keys, and check clauses from Step 5.
   - Add `COMMENT ON TABLE` and `COMMENT ON COLUMN` statements where clarity helps future maintainers.
6. Create indexes and unique constraints after table definitions to keep the script readable.
7. Use `${schema}` token in every relation reference (e.g., `${schema}.xp_users`) to match Identity patterns.
8. End with any optional `ANALYZE` or data seeding decisions (for this migration, no seed data).

---

## Step 7 – DbmService Wiring Task List

To validate migrations without manual `psql`, mirror the Identity repo’s persistence wiring:

1. **Create Infrastructure Project** – Add `src/PlayerEngagement.Infrastructure/PlayerEngagement.Infrastructure.csproj` referencing `InnoAndLogic.Persistence` and embedding `Persistence/Migrations/*.sql`.
2. **Define Interfaces** – Introduce `Persistence/IPlayerEngagementDbmService.cs` describing the persistence surface (even if it only exposes a health check initially) to keep parity with Identity.
3. **Implement `PlayerEngagementDbmService`** – Derive from `DbmService`, inject `PostgresExecutor`, `DatabaseOptions`, `DbMigrations`; invoke the base constructor so migrations apply on activation.
4. **Optional In-Memory Stub** – Add `PlayerEngagementDbmInMemoryService` for tests/development parity with Identity (can return `Result.Success`).
5. **Host Configuration Extension** – Create `PlayerEngagementDbmHostConfig.ConfigurePlayerEngagementPersistenceServices` extension method that binds `DatabaseOptions`, registers `DbMigrations`, and wires either the real or in-memory service based on `DatabaseProvider`.
6. **Embed Migration Assembly** – Ensure the extension accepts external migration assemblies and defaults to `PlayerEngagement.Infrastructure` assembly so embedded SQL is discoverable.
7. **Update Host Startup** – In `src/PlayerEngagement.Host/Program.cs`, call `services.ConfigurePlayerEngagementPersistenceServices(configuration, "DbmOptions", new[]{ typeof(PlayerEngagement.Infrastructure.Persistence.PlayerEngagementDbmService).Assembly })` and keep a scope that resolves `IPlayerEngagementDbmService` at startup.
8. **Configuration Files** – Add a `DbmOptions` section in `appsettings.json`/`appsettings.Development.json` (provider, connection string, schema) or map existing Postgres settings to `DatabaseOptions` via configuration binding.
9. **Project References & Build** – Reference `PlayerEngagement.Infrastructure` from `PlayerEngagement.Host` (and solution) so DI wiring compiles.
10. **Verify Run** – Build and run the host; the `DbmService` constructor should execute migrations automatically against the compose Postgres instance.
