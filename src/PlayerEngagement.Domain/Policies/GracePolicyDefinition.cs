namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Defines how many missed days are tolerated, and over what window, before a streak resets.
/// </summary>
/// <param name="AllowedMisses">Number of missed days allowed within the window.</param>
/// <param name="WindowDays">Number of consecutive days across which misses are evaluated.</param>
public sealed record GracePolicyDefinition(int AllowedMisses, int WindowDays);
