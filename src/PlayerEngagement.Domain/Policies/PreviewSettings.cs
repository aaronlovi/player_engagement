namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Settings used by operator tooling when previewing a policy's projected rewards.
/// </summary>
/// <param name="SampleWindowDays">Number of days to include when visualizing the reward curve.</param>
/// <param name="DefaultSegment">Optional default segment key used for simulations.</param>
public sealed record PreviewSettings(int SampleWindowDays, string? DefaultSegment);
