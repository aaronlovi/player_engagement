# Soft Decay Streak Model (TR-02 Detail)

## Rule Summary

- Decay applies after grace is exhausted.
- Effective streak day after a miss: `next = max(1, floor(current_streak * (1 - decayPercent)))`.
- Use `next` to pick the streak curve entry; XP = `base_xp * curve_multiplier (+ additive bonus)` as defined in the policy.

## Walkthrough Example

Assumptions: base XP = 100; DecayPercent = 25%; streak curve multipliers per day index: 0→1.0, 1→1.1, 2→1.2, 3→1.3, 4→1.4, 5→1.5, 6→1.6, 7+→1.7 (plateau).

Scenario: player had streak day 7 yesterday, misses one reward day, then claims today (grace already used).

1) Apply decay: `next = max(1, floor(7 * 0.75)) = max(1, 5.25 floor) = 5`.
2) Streak curve lookup: day index 5 → multiplier 1.5.
3) XP granted: `100 * 1.5 = 150` (plus any additive bonus for day 5 if configured).

Other rounding choices for comparison (not selected):

- Ceiling: `ceil(5.25) = 6` → multiplier 1.6 → XP 160 (gentler).
- Round-to-nearest: `round(5.25) = 5` → multiplier 1.5 → XP 150 (similar to floor here).
- Hard reset: set `next = 1` → multiplier 1.1 → XP 110 (harshest).

Chosen behavior: floor and clamp to at least 1 for predictable, conservative decay.
