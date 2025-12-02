# TR-01 Policy-as-Data — Data Model Specification

## Scope
Defines the database structures required to persist policy-as-data for the Daily Login XP grant in the local development environment (Postgres via Docker Desktop). The design favors relational columns for queryable fields and uses JSONB only for model-specific configuration values unlikely to drive direct SQL filtering.

## Key Principles
- **Strong contracts:** Core policy attributes (base awards, claim windows, grace rules, streak curve entries, seasonal boosts, segment mappings) live in first-class tables/columns.
- **Immutable history:** Published policy versions remain immutable; new versions append rows.
- **Single active version:** Each `policy_key` has at most one published (`status = 'Published'`) version.
- **Local-first:** No multi-region assumptions; timestamps stored as `timestamptz`.

## Tables

### `xp_policies`
Holds metadata for each logical policy (e.g., `daily-login-xp`).

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| `policy_key` | `text` | PK | Stable identifier, lowercase slug. |
| `display_name` | `text` | NOT NULL | Operator-facing name. |
| `description` | `text` | NULL | Optional summary. |
| `created_at` | `timestamptz` | NOT NULL, default `now_utc()` | |
| `created_by` | `text` | NOT NULL | Operator/system creating policy shell. |

### `xp_policy_versions`
Stores immutable versions for each policy.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| `policy_key` | `text` | FK → `xp_policies(policy_key)` | |
| `policy_version` | `int` | NOT NULL | Version sequence per policy. PK with `policy_key`. |
| `status` | `text` | CHECK (`status` IN ('Draft','Published','Archived')) | |
| `base_xp_amount` | `int` | CHECK > 0 | Base award amount. |
| `currency` | `text` | CHECK (`currency` = 'XP') | Future extension point. |
| `claim_window_start_minutes` | `int` | 0 ≤ value < 1440 | Minutes offset from midnight in anchor tz. |
| `claim_window_duration_hours` | `int` | 1 ≤ value ≤ 24 | |
| `anchor_strategy` | `text` | ENUM-like CHECK | Values: `ANCHOR_TIMEZONE`,`FIXED_UTC`,`SERVER_LOCAL`. |
| `grace_allowed_misses` | `int` | ≥ 0 | |
| `grace_window_days` | `int` | ≥ `grace_allowed_misses` | |
| `streak_model_type` | `text` | CHECK (value in supported models) | Matches domain enum. |
| `streak_model_parameters` | `jsonb` | NOT NULL default `'{}'` | Model-specific configuration (kept in JSONB). |
| `preview_sample_window_days` | `int` | ≥ 1 | Default 14. |
| `preview_default_segment` | `text` | NULL | |
| `seasonal_metadata` | `jsonb` | NOT NULL default `'{}'` | Lightweight summary for UI (denormalized). |
| `effective_at` | `timestamptz` | NULL | When version becomes active. |
| `superseded_at` | `timestamptz` | NULL | Set on archive. |
| `created_at` | `timestamptz` | NOT NULL default `now_utc()` | |
| `created_by` | `text` | NOT NULL | Operator identity. |
| `published_at` | `timestamptz` | NULL | Set when status transitions to `Published`. |

Constraints/Indexes:
- PK on (`policy_key`, `policy_version`).
- Unique partial index ensuring only one published version: `CREATE UNIQUE INDEX xp_policy_versions_published_uidx ON ... (policy_key) WHERE status = 'Published';`
- Index on (`policy_key`, `status`) for quick status queries.

### `xp_policy_streak_curve`
Represents streak multipliers/bonuses per day index.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| `policy_key` | `text` | FK → `xp_policy_versions` | Part of composite FK with `policy_version`. |
| `policy_version` | `int` | FK → `xp_policy_versions` | |
| `day_index` | `int` | ≥ 0 | PK with `policy_key`, `policy_version`. |
| `multiplier` | `numeric(8,4)` | ≥ 0 | |
| `additive_bonus_xp` | `int` | ≥ 0 | |
| `cap_next_day` | `boolean` | NOT NULL default false | Signals plateau cap. |

Additional constraint: `EXCLUDE USING gist` or trigger to enforce contiguous day_index (implemented in app logic; DB check ensures `day_index` unique per version).

### `xp_policy_seasonal_boosts`

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| `policy_key` | `text` | FK | Part of composite FK with `policy_version`. |
| `policy_version` | `int` | FK | |
| `boost_id` | `bigserial` | PK | |
| `label` | `text` | NOT NULL | |
| `multiplier` | `numeric(8,4)` | ≥ 1 | |
| `start_utc` | `timestamptz` | NOT NULL | |
| `end_utc` | `timestamptz` | NOT NULL, CHECK `end_utc > start_utc` | |

Unique partial index preventing overlapping ranges per version can be enforced via exclusion constraint:  
`EXCLUDE USING gist (policy_key WITH =, policy_version WITH =, tstzrange(start_utc, end_utc) WITH &&)`

### `xp_policy_segment_overrides`
Maps player segments to specific policy versions.

| Column | Type | Constraints | Notes |
| --- | --- | --- | --- |
| `segment_key` | `text` | PK component | |
| `policy_key` | `text` | FK → `xp_policies` | |
| `target_policy_version` | `int` | FK → `xp_policy_versions` | Composite FK (`policy_key`, `target_policy_version`). Must reference `status = 'Published'`. |
| `created_at` | `timestamptz` | NOT NULL default `now_utc()` | |
| `created_by` | `text` | NOT NULL | |

Optional: include `priority` column later if overlapping segments need ordering.

### Existing Tables
- `xp_awards` remains unchanged, continuing to store `policy_key`/`policy_version` references.
- `xp_rules` table will be deprecated/replaced by the new structures; migration will move any seed data if introduced later.

## JSONB Usage
- `streak_model_parameters`: model-specific payload (e.g., tier arrays). Not expected to drive SQL filters; consumers deserialize in application layer.
- `seasonal_metadata`: optional UI hints (e.g., colors, copy). Derived data; not used for filtering.

## Migration Plan
1. **Create core tables:** `xp_policies`, `xp_policy_versions`, `xp_policy_streak_curve`, `xp_policy_seasonal_boosts`, `xp_policy_segment_overrides`.
2. **Backfill** (if historical data exists): migrate rows from `xp_rules` into new tables. For greenfield, simply seed initial policy shell.
3. **Update foreign keys:** No changes needed for `xp_awards`; columns already reference `policy_key`/`policy_version`.
4. **Deprecate legacy table:** Drop `xp_rules` after confirming no dependencies (or rename for archival).
5. **Ensure idempotency:** Migration should be additive and safe to rerun; wrap in transaction.

## Query Patterns
- Fetch active policy: `SELECT * FROM xp_policy_versions WHERE policy_key = $1 AND status = 'Published'`.
- Load streak curve: join on `xp_policy_streak_curve`.
- Seasonal boosts and segment overrides loaded via FK queries.
- Analytics: `xp_awards` continues to aggregate by `policy_key`, `policy_version`.

## Future Considerations
- If milestone rewards require joins to cosmetics, add linking table referencing external inventory IDs.
- Segment catalog source to be defined in Step 7; current table assumes `segment_key` is authoritative in application layer.
- For soft-delete/governance policies, add nullable `retired_at` columns; not required initially.
