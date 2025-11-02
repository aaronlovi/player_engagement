# Scaffolding Follow-Ups

## Task 3 – `V001__init_schema.sql`

- **Timezone change history on `xp_users`**  
  Capture how reward timezone changes are audited (trigger vs. history table) before exposing admin tooling. Source: `task3_init_schema_plan.md` notes around user metadata.

- **Balance trigger contract**  
  Confirm the upcoming `xp.fn_apply_ledger()` trigger consumes signed `xp_ledger.amount` values (credits positive, debits negative) and/or introduce an explicit `is_credit` flag so balance math stays deterministic. Align this design before starting Task 4.

- **Canonical `reward_day_id` format**  
  Decide on the storage convention (`YYYY-MM-DD`, `YYYYMMDD`, tz suffix, etc.) shared by `xp_streaks` and `xp_awards`, then update the migration and downstream DTOs accordingly.

- **Optional analytics indexes**  
  Re-evaluate `xp_streaks_last_reward_day_idx`, `xp_awards_reward_day_idx`, and other “optional” indexes once repository query patterns are defined to avoid unnecessary bloat.
