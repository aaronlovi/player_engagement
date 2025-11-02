# Scaffolding Follow-Ups

## Task 3 – `V001__init_schema.sql`

### Immediate Close-Out Checklist

- [x] **Ledger debit semantics** — Keep `xp_ledger.amount` as a signed (non-zero) column with no separate credit flag; ensure docs/specs call out that negative entries represent debits.
- [ ] **Balance trigger contract** — With the ledger decision in hand, outline how `xp.fn_apply_ledger()` will consume those columns so Task 4 begins with a locked spec.
- [ ] **Canonical `reward_day_id` format** — Choose the storage convention shared by `xp_streaks` and `xp_awards` (e.g., `YYYY-MM-DD` in UTC) and reflect it in the migration plus DTO plans.

### Deferred Until Downstream Tasks

- [ ] **Optional analytics indexes** — Re-evaluate `xp_streaks_last_reward_day_idx`, `xp_awards_reward_day_idx`, and similar indexes after repository query patterns settle.
- [ ] **Timezone change audit strategy** — Capture how reward timezone updates are recorded (trigger vs. history table) before implementing admin tooling for timezone changes.
