namespace PlayerEngagement.Domain.Policies;

/// <summary>Lifecycle states for policy versions.</summary>
public enum PolicyVersionStatus {
    /// <summary>Undefined status; treat as uninitialized.</summary>
    Invalid = 0,
    /// <summary>Draft version not yet published.</summary>
    Draft = 1,
    /// <summary>Version currently published/active.</summary>
    Published = 2,
    /// <summary>Version archived/superseded.</summary>
    Archived = 3
}

/// <summary>Anchor strategies for reward-day computation.</summary>
public enum AnchorStrategy {
    /// <summary>Undefined anchor strategy.</summary>
    Invalid = 0,
    /// <summary>Anchor reward day on the player's configured timezone.</summary>
    AnchorTimezone = 1,
    /// <summary>Anchor reward day using a fixed UTC offset regardless of player timezone.</summary>
    FixedUtc = 2,
    /// <summary>Anchor reward day using the server's local time.</summary>
    ServerLocal = 3
}

/// <summary>Supported streak model types.</summary>
public enum StreakModelType {
    /// <summary>Undefined streak model.</summary>
    Invalid = 0,
    /// <summary>Plateau/Cap model where growth stops after a threshold.</summary>
    PlateauCap = 1,
    /// <summary>Weekly cycle reset model.</summary>
    WeeklyCycleReset = 2,
    /// <summary>Decay curve model that soft-resets progress.</summary>
    DecayCurve = 3,
    /// <summary>Tiered seasonal reset model.</summary>
    TieredSeasonalReset = 4,
    /// <summary>Milestone/meta reward model.</summary>
    MilestoneMetaReward = 5
}
