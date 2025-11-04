# TR-01 Policy-as-Data — Domain Model

## Aggregate Overview

```
PolicyAggregate
└── PolicyDescriptor (policyKey, displayName, description)
└── PolicyVersion (version, status, createdAt, createdBy)
    ├── BaseAward
    │   └── amountXp
    │   └── currency (XP only for now; reserved for future)
    ├── StreakCurve[]
    │   └── dayIndex (0-based)
    │   └── multiplier (decimal)
    │   └── additiveBonusXp (int)
    ├── GracePolicy
    │   └── allowedMisses (int)
    │   └── windowDays (int)
    ├── ClaimWindow
    │   └── startLocalTime (HH:mm)
    │   └── durationHours (int)
    │   └── anchorTimezoneStrategy (enum: AnchorTimezone, FixedUtc, ServerLocal)
    ├── StreakModel
    │   └── type (enum)
    │   └── parameters (JSON blob typed per model)
    ├── SeasonalBoosts[]
    │   └── label
    │   └── multiplier
    │   └── startUtc
    │   └── endUtc
    ├── SegmentOverrides[]
    │   └── segmentKey
    │   └── policyVersionRef (version number)
    └── PreviewSettings
        └── sampleWindowDays
        └── defaultUserSegment
```

- `PolicyAggregate` is identified by `policyKey` (e.g., `daily-login-xp`).  
- Each `PolicyVersion` is immutable once published. Draft versions may be deleted before publication.  
- Only one `PolicyVersion` per `policyKey` may have `status = Published`. Previous published versions transition to `status = Archived`.

## Entities and Value Objects

### PolicyDescriptor
- `policyKey` (string, slug) — shared identifier across versions.
- `displayName` (string) — operator-facing name.
- `description` (string) — optional summary of usage.

### PolicyVersion
- `version` (int, auto-increment per policyKey).
- `status` (`Draft`, `Published`, `Archived`).
- `createdAt`, `createdBy` (metadata).
- `effectiveAt` (optional); defaults to immediate.
- `supersededAt` (optional; set when a newer `Published` version exists).
- `definition` (see sub-components below).

### BaseAward
- `amountXp` (int > 0).
- `currency` (enum: `XP`). Future proofing for coins.

### StreakCurveEntry
- `dayIndex` (int ≥ 0).
- `multiplier` (decimal ≥ 0). Applied to base XP.
- `additiveBonusXp` (int ≥ 0). Adds to multiplier output.
- `capNextDay` (optional bool) to cap growth at threshold (for Plateau/Cap model).

### GracePolicy
- `allowedMisses` (int ≥ 0).
- `windowDays` (int ≥ `allowedMisses`). Defines look-back span for grace.

### ClaimWindow
- `startLocalTime` (ISO `HH:mm`).
- `durationHours` (int between 1 and 24; default 24).
- `anchorStrategy`:
  - `AnchorTimezone` (per-user timezone anchored, default).
  - `FixedUtc` (server-controlled UTC window).
  - `ServerLocal` (fallback when timezone missing).

### StreakModel
`type` enumerates supported requirement models:
- `PlateauCap`
- `WeeklyCycleReset`
- `DecayCurve`
- `TieredSeasonalReset`
- `MilestoneMetaReward`

`parameters` are typed objects per model. Examples:
- `PlateauCap`: `{ plateauDay:int, plateauMultiplier:decimal }`
- `WeeklyCycleReset`: `{ cycleLength:int }`
- `DecayCurve`: `{ decayPercent:decimal, graceDay:int }`
- `TieredSeasonalReset`: `{ tiers:[{ startDay:int, endDay:int, bonusMultiplier:decimal }] }`
- `MilestoneMetaReward`: `{ milestones:[{ day:int, rewardType:string, rewardValue:string }] }`

### SeasonalBoost
- `label` (string).
- `multiplier` (decimal ≥ 1).
- `startUtc`, `endUtc` (timestamp).

### SegmentOverride
- `segmentKey` (string identifier).
- `policyVersionRef` (int). Points to another `PolicyVersion` for that segment. Defaults to the current version.

### PreviewSettings
- `sampleWindowDays` (int) — default horizon for UI preview graphs.
- `defaultUserSegment` — optional segment key used when simulating.

## Aggregation Rules
- `PolicyAggregate` enforces that only one `Published` version exists at a time.
- Publishing a draft increments version and archives previous published version.
- Segment overrides must reference versions with `status = Published`.
- Seasonal boosts cannot overlap; enforce via validation.
- Claim window plus anchor strategy must cover 24 hours collectively for fairness; enforce `durationHours <= 24`.

## State Transitions
1. **Draft Creation**  
   Operators create drafts. Status = `Draft`; version assigned but hidden from claim services.
2. **Validation**  
   Domain service validates definitions, including streak curve monotonicity constraints and claim window bounds.
3. **Publish**  
   Transitions status to `Published`, sets `effectiveAt`. Previous `Published` → `Archived`.
4. **Retire**  
   Optional: mark `Published` version as `Archived` without replacement (used when disabling feature).

## Validation Rules
- `amountXp > 0`.
- `multiplier >= 0`. Combined base * multiplier + additive must remain within configured XP cap (configurable constant, default 5000 XP).
- `dayIndex` entries must start at 0 and increase by 1 without gaps.
- `allowedMisses <= windowDays`.
- `startUtc < endUtc` for seasonal boosts; ranges must not overlap.
- `effectiveAt` cannot be in the past at publish time (local dev scenario can allow immediate).
- `segmentKey` must exist in segment catalog (to be defined in Step 7).

## Serialization Considerations
- Policy definitions are stored as JSON (`xp_rules.definition`) for flexibility.
- Use System.Text.Json with explicit `JsonSchema` for validation.
- For local dev, persistence type is Postgres with JSONB columns.

## Integration Points
- **Policy Service** loads `PolicyVersion` by key/version for API responses.
- **Daily Login Service** requests the current `Published` version (and segment override, if applicable) to compute claims.
- **Admin UI** consumes domain DTOs for policy editing and preview simulation.
- **Telemetry** attaches `policyKey`, `policyVersion` to claim events for analytics.

## Open Questions
- Do milestones require separate inventory integration (cosmetics, currency)? (Track for later requirement.)
- Should `SegmentOverride` allow nested overrides (segment + seasonal)? For now, single-level mapping suffices.
