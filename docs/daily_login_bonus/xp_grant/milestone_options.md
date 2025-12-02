# Milestone Reward Handling Options (TR-02)

Current milestone model: `MilestoneMetaRewardMilestone` carries `Day`, `RewardType`, and `RewardValue` (free-form strings). Below are handling options with examples; current choice is highlighted.

## Options

1) **XP-only modifier (simplest)**  
   - Treat milestone as extra XP (additive or multiplier) folded into the award.  
   - Example: Day 7 milestone → add +50 XP on top of streak curve.  
   - Pros: no new systems; pure XP path. Cons: no cosmetic/unlock semantics.

2) **XP + flag (current choice)**  
   - Grant normal streak XP, plus mark the milestone in award metadata/receipt and streak state for idempotency.  
   - Example: Day 7 milestone → XP from streak curve; award metadata includes `{ milestone_day:7, rewardType:"badge", rewardValue:"golden-login" }`; streak state stores milestone id to avoid re-award on retries.  
   - Pros: clients can show “milestone reached”; retries safe; no external dependency. Cons: actual item delivery deferred.

3) **Side reward event (deferred for now)**  
   - Emit domain event when milestone hits: `{user_id, reward_day_id, receipt_id, policy_version, milestone_day, rewardType, rewardValue}`.  
   - Example: Day 7 milestone → emit `MilestoneUnlocked` event; downstream inventory service consumes later.  
   - Pros: decouples delivery; future-friendly for cosmetics. Cons: needs consumer + idempotency tracking; currently deferred.

4) **Persistence log/table**  
   - Insert `milestone_rewards` row (user_id, reward_day_id, milestone_day/id, rewardType/value, receipt_id, policy_version).  
   - Example: Day 7 milestone → row records rewardType/value; used later to deliver items.  
   - Pros: audit trail and idempotency. Cons: extra schema and write path; may be premature.

## Current Stance

- Use **Option 2 (XP + flag)**: apply XP normally, record milestone in award metadata/state, and prevent duplicate milestone grants on retries.  
- Defer external events or side tables until non-XP assets exist.  
- `RewardType/RewardValue` stay flexible strings until a real asset catalog/inventory path is defined.
