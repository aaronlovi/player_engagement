# Balance Projection Contract

> Captures the Orleans-side workflow for keeping `${schema}.xp_balance` in sync with `${schema}.xp_ledger` once Task 4 begins. No database triggers are used—projection logic lives entirely in C#.

## Execution Model

- The per-user Orleans grain (or equivalent command handler) is the single writer for ledger and balance mutations. Background services are not required.
- On activation, the grain loads the user’s `xp_balance` row (creating one with zeros if it does not exist yet) and primes an in-memory snapshot that includes the last-known ledger timestamp and outstanding seasonal context.
- Each grant/correction processes sequentially:
  1. Begin a database transaction using the shared Npgsql connection factory.
  2. Insert the new `xp_ledger` row with its signed `amount`, `policy_key`, `policy_version`, `season_id`, `metadata`, and `correlation_id`.
  3. Select the current balance row `FOR UPDATE` to guard against concurrent writes and to honor the `current_balance >= 0` constraint.
  4. Apply the projection rules (below) to compute the updated balance fields.
  5. Persist the updated balance (`current_balance`, `lifetime_xp`, `seasonal_xp`, `season_id`, `updated_at`) and commit.
  6. Update the grain’s in-memory snapshot so subsequent messages operate on fresh state without re-querying.

## Amount Handling

- `xp_ledger.amount` remains a signed, non-zero bigint. Positive values are credits; negative values are debits.
- `xp_balance.current_balance` always adds the signed `amount`.
- `xp_balance.lifetime_xp` increases only when the ledger `amount > 0`. Negative amounts do **not** reduce lifetime totals by default.
- Administrative debits that must reverse lifetime XP include the metadata flag `{"affectsLifetime": true}` (exact casing TBD). When that flag is present, the projection subtracts the absolute value from `lifetime_xp` as well.
- The grain validates that `current_balance` does not fall below zero after the update; if it would, it rejects the operation (or requires an explicit override flag) to keep the existing check constraint satisfied.

## Seasonal XP

- The balance row tracks the active season in `season_id`. Seasonal XP updates follow these rules:
  - If the incoming ledger row has a `season_id` and it matches the balance’s `season_id`, add the signed amount to `seasonal_xp`.
  - If the ledger row has a `season_id` that differs from the balance row, first reset `seasonal_xp` to zero, update `season_id` to the new value, and then apply the amount.
  - If the ledger row omits `season_id`, leave `seasonal_xp` unchanged.
- Future work can split seasonal tracking into a dedicated table if we outgrow this projection.

## Backdated or Out-of-Order Entries

- Ledger inserts always succeed in arrival order through the grain, but operators may backdate `created_at` for corrections.
- When a new ledger entry’s `created_at` is earlier than the grain’s last-seen ledger timestamp, the grain still applies the incremental update but also emits a structured log entry (event id `XP_BALANCE_REBUILD_REQUIRED`) so operators can queue a rebuild manually if needed.
- A full rebuild routine (outside this task) can recompute balances via `SUM(xp_ledger.amount)` for the affected user to guarantee eventual consistency.

## Concurrency & Idempotency

- Orleans single-writer semantics prevent concurrent grain execution, but the projection additionally locks the `xp_balance` row with `SELECT ... FOR UPDATE` to avoid violating constraints if external processes ever touch the table.
- The unique `xp_ledger.correlation_id` constraint enforces idempotency. The grain should catch `unique_violation` and treat it as a no-op when replaying commands.

## Outstanding Clarifications

- JSON metadata key for lifetime-reducing debits: `affectsLifetime` (boolean).
- “Balance rebuild required” signal: structured log entry; revisit for automation later.
