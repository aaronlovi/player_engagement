# Scaffolding Follow-Ups

## Task 3 – `V001__init_schema.sql`

### Immediate Close-Out Checklist

- [x] **Ledger debit semantics** — Keep `xp_ledger.amount` as a signed (non-zero) column with no separate credit flag; ensure docs/specs call out that negative entries represent debits.
- [x] **Balance projection contract** — Documented in `docs/scaffolding/balance_projection_contract.md`; Task 4 should implement the Orleans grain workflow described there.
- [x] **Canonical `reward_day_id` format** — Store reward days as `YYYY-MM-DD` strings interpreted in the player’s reward timezone at grant time; migration comments updated to match.

### Deferred Until Downstream Tasks

- [ ] **Optional analytics indexes** — Re-evaluate `xp_streaks_last_reward_day_idx`, `xp_awards_reward_day_idx`, and similar indexes after repository query patterns settle.
- [ ] **Timezone change audit strategy** — Capture how reward timezone updates are recorded (trigger vs. history table) before implementing admin tooling for timezone changes.
