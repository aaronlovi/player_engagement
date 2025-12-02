export type PolicyVersionStatus = 'Draft' | 'Published' | 'Archived';

export type AnchorStrategy = 'ANCHOR_TIMEZONE' | 'FIXED_UTC' | 'SERVER_LOCAL';

export type StreakModelType =
  | 'PlateauCap'
  | 'WeeklyCycleReset'
  | 'DecayCurve'
  | 'TieredSeasonalReset'
  | 'MilestoneMetaReward';

export interface PlateauCapParameters {
  plateauDay: number;
  plateauMultiplier: number;
}

export type WeeklyCycleResetParameters = Record<string, never>;

export interface DecayCurveParameters {
  decayPercent: number;
  graceDay: number;
}

export interface TierParameters {
  startDay: number;
  endDay: number;
  bonusMultiplier: number;
}

export interface TieredSeasonalResetParameters {
  tiers: TierParameters[];
}

export interface MilestoneParameters {
  day: number;
  rewardType: string;
  rewardValue: string;
}

export interface MilestoneMetaRewardParameters {
  milestones: MilestoneParameters[];
}

export type StreakModelParameters =
  | PlateauCapParameters
  | WeeklyCycleResetParameters
  | DecayCurveParameters
  | TieredSeasonalResetParameters
  | MilestoneMetaRewardParameters;
