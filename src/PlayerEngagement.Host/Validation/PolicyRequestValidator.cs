using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Host.Contracts.Policies;

namespace PlayerEngagement.Host.Validation;

/// <summary>
/// Centralizes API-level validation for policy controller requests.
/// </summary>
internal static partial class PolicyRequestValidator {
    private const int MaxDisplayNameLength = 128;
    private const int MaxDescriptionLength = 1024;
    private const int MaxGraceWindowDays = 14;
    private const int MaxCurrencyLength = 8;
    private const int MinCurrencyLength = 3;
    private const int MinBaseXp = 1;
    private const int MaxBaseXp = 10000;
    private const int MinPreviewWindow = 1;
    private const int MaxListLimit = 200;
    [GeneratedRegex("^[a-z0-9_-]{3,64}$", RegexOptions.Compiled)] private static partial Regex CreatePolicyKeyRegex();
    [GeneratedRegex("^[A-Z]{3,8}$", RegexOptions.Compiled)] private static partial Regex CreateCurrencyRegex();
    [GeneratedRegex("^[A-Za-z0-9_]{1,32}$", RegexOptions.Compiled)] private static partial Regex CreateMyRegex();
    private static readonly Regex PolicyKeyRegex = CreatePolicyKeyRegex();
    private static readonly Regex CurrencyRegex = CreateCurrencyRegex();
    private static readonly Regex SegmentKeyRegex = CreateMyRegex();
    private static readonly TimeSpan PublishBackdateTolerance = TimeSpan.FromMinutes(1);

    internal static bool TryValidateCreate(
        string policyKey,
        CreatePolicyVersionRequest? request,
        out IDictionary<string, string[]>? errors) {

        Dictionary<string, List<string>> bag = CreateBag();
        ValidatePolicyKey(policyKey, bag);
        if (request is null) {
            AddError(bag, "body", "Request payload is required.");
            return BuildResult(bag, out errors);
        }

        ValidateDisplayFields(request, bag);
        ValidateCurrency(request, bag);
        ValidateClaimWindow(request, bag);
        ValidateGracePolicy(request, bag);
        ValidateAnchorStrategy(request, bag);
        ValidateStreakModel(request, bag);
        ValidatePreviewSettings(request, bag);
        ValidateStreakCurve(request, bag);
        ValidateSeasonalBoosts(request, bag);
        ValidateEffectiveAt(request.EffectiveAt, "effectiveAt", bag);

        return BuildResult(bag, out errors);
    }

    internal static bool TryValidatePublish(
        string policyKey,
        int policyVersion,
        PublishPolicyVersionRequest? request,
        out IDictionary<string, string[]>? errors) {

        Dictionary<string, List<string>> bag = CreateBag();
        ValidatePolicyKey(policyKey, bag);
        ValidatePositiveVersion(policyVersion, bag);

        DateTime? effectiveAt = request?.EffectiveAt;
        ValidateEffectiveAt(effectiveAt, "effectiveAt", bag);

        if (request?.SegmentOverrides is not null && request.SegmentOverrides.Count > 0)
            ValidateSegmentOverrides(request.SegmentOverrides, bag);

        return BuildResult(bag, out errors);
    }

    internal static bool TryValidateRetire(
        string policyKey,
        int policyVersion,
        RetirePolicyVersionRequest? request,
        out IDictionary<string, string[]>? errors) {

        Dictionary<string, List<string>> bag = CreateBag();
        ValidatePolicyKey(policyKey, bag);
        ValidatePositiveVersion(policyVersion, bag);

        if (request?.RetiredAt is { } retiredAt && retiredAt > DateTime.UtcNow)
            AddError(bag, "retiredAt", "retiredAt cannot be in the future.");

        return BuildResult(bag, out errors);
    }

    internal static bool TryValidateVersionLookup(
        string policyKey,
        int policyVersion,
        out IDictionary<string, string[]>? errors) {

        Dictionary<string, List<string>> bag = CreateBag();
        ValidatePolicyKey(policyKey, bag);
        ValidatePositiveVersion(policyVersion, bag);
        return BuildResult(bag, out errors);
    }

    internal static bool TryValidateListQuery(
        string policyKey,
        string? status,
        DateTime? effectiveBefore,
        int? limit,
        out IDictionary<string, string[]>? errors) {

        Dictionary<string, List<string>> bag = CreateBag();
        ValidatePolicyKey(policyKey, bag);

        if (!string.IsNullOrWhiteSpace(status) &&
            !Enum.TryParse(status, true, out PolicyVersionStatus _)) {
            AddError(bag, "status", "Status must match PolicyVersionStatus values (Draft, Published, Archived).");
        }

        if (limit.HasValue && (limit.Value <= 0 || limit.Value > MaxListLimit))
            AddError(bag, "limit", $"limit must be between 1 and {MaxListLimit}.");

        if (effectiveBefore.HasValue && effectiveBefore.Value.Kind == DateTimeKind.Unspecified)
            AddError(bag, "effectiveBefore", "effectiveBefore must include a UTC designator.");

        return BuildResult(bag, out errors);
    }

    internal static bool TryValidateActiveQuery(
        string? policyKey,
        string? segment,
        out IDictionary<string, string[]>? errors) {

        Dictionary<string, List<string>> bag = CreateBag();
        if (string.IsNullOrWhiteSpace(policyKey))
            AddError(bag, "policyKey", "policyKey is required.");
        else
            ValidatePolicyKey(policyKey!, bag);

        if (!string.IsNullOrWhiteSpace(segment) && !SegmentKeyRegex.IsMatch(segment!))
            AddError(bag, "segment", "segment must be alphanumeric/underscore up to 32 characters.");

        return BuildResult(bag, out errors);
    }

    internal static bool TryValidateSegmentOverrides(
        string policyKey,
        out IDictionary<string, string[]>? errors) {

        Dictionary<string, List<string>> bag = CreateBag();
        ValidatePolicyKey(policyKey, bag);
        return BuildResult(bag, out errors);
    }

    internal static bool TryValidateSegmentOverrideUpdate(
        string policyKey,
        UpdateSegmentOverridesRequest? request,
        out IDictionary<string, string[]>? errors) {

        Dictionary<string, List<string>> bag = CreateBag();
        ValidatePolicyKey(policyKey, bag);
        if (request?.Overrides is null || request.Overrides.Count == 0) {
            AddError(bag, "overrides", "At least one segment override must be provided.");
        } else {
            ValidateSegmentOverrides(request.Overrides, bag);
        }

        return BuildResult(bag, out errors);
    }

    private static Dictionary<string, List<string>> CreateBag() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static void ValidateDisplayFields(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            AddError(bag, "displayName", "DisplayName is required.");
        else if (request.DisplayName.Length > MaxDisplayNameLength)
            AddError(bag, "displayName", $"DisplayName cannot exceed {MaxDisplayNameLength} characters.");

        if (request.Description is { Length: > MaxDescriptionLength })
            AddError(bag, "description", $"Description cannot exceed {MaxDescriptionLength} characters.");
    }

    private static void ValidateCurrency(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (string.IsNullOrWhiteSpace(request.Currency))
            AddError(bag, "currency", "Currency is required.");
        else if (!CurrencyRegex.IsMatch(request.Currency))
            AddError(bag, "currency", $"Currency must be uppercase letters ({MinCurrencyLength}-{MaxCurrencyLength} chars).");

        if (request.BaseXpAmount is < MinBaseXp or > MaxBaseXp)
            AddError(bag, "baseXpAmount", $"BaseXpAmount must be between {MinBaseXp} and {MaxBaseXp}.");
    }

    private static void ValidateClaimWindow(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (request.ClaimWindowStartMinutes is < 0 or >= 1440)
            AddError(bag, "claimWindowStartMinutes", "ClaimWindowStartMinutes must be between 0 and 1439.");
        if (request.ClaimWindowDurationHours is < 1 or > 24)
            AddError(bag, "claimWindowDurationHours", "ClaimWindowDurationHours must be between 1 and 24.");
    }

    private static void ValidateGracePolicy(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (request.GraceAllowedMisses < 0)
            AddError(bag, "graceAllowedMisses", "GraceAllowedMisses cannot be negative.");
        if (request.GraceWindowDays < request.GraceAllowedMisses)
            AddError(bag, "graceWindowDays", "GraceWindowDays must be >= GraceAllowedMisses.");
        if (request.GraceWindowDays > MaxGraceWindowDays)
            AddError(bag, "graceWindowDays", $"GraceWindowDays cannot exceed {MaxGraceWindowDays}.");
    }

    private static void ValidateAnchorStrategy(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (string.IsNullOrWhiteSpace(request.AnchorStrategy)) {
            AddError(bag, "anchorStrategy", "AnchorStrategy is required.");
            return;
        }

        if (!Enum.TryParse(request.AnchorStrategy, true, out AnchorStrategy anchor) || anchor == AnchorStrategy.Invalid)
            AddError(bag, "anchorStrategy", "AnchorStrategy must match a known value.");
    }

    private static void ValidateStreakModel(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (string.IsNullOrWhiteSpace(request.StreakModelType) ||
            !Enum.TryParse(request.StreakModelType, true, out StreakModelType model) ||
            model == StreakModelType.Invalid) {
            AddError(bag, "streakModelType", "StreakModelType must match a known value.");
        }


        if (request.StreakModelParameters is null)
            AddError(bag, "streakModelParameters", "StreakModelParameters are required.");
    }

    private static void ValidatePreviewSettings(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (request.PreviewSampleWindowDays < MinPreviewWindow)
            AddError(bag, "previewSampleWindowDays", $"PreviewSampleWindowDays must be >= {MinPreviewWindow}.");

        if (!string.IsNullOrWhiteSpace(request.PreviewDefaultSegment) &&
            !SegmentKeyRegex.IsMatch(request.PreviewDefaultSegment)) {
            AddError(bag, "previewDefaultSegment", "PreviewDefaultSegment must be alphanumeric/underscore up to 32 characters.");
        }
    }

    private static void ValidateStreakCurve(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (request.StreakCurve is null || request.StreakCurve.Count == 0) {
            AddError(bag, "streakCurve", "At least one streak curve entry is required.");
            return;
        }

        int expectedDay = 0;
        foreach (StreakCurveEntry entry in request.StreakCurve) {
            if (entry.DayIndex != expectedDay)
                AddError(bag, "streakCurve", $"Streak curve day index {entry.DayIndex} must be sequential starting at 0.");
            if (entry.Multiplier <= 0)
                AddError(bag, "streakCurve", "Streak curve multipliers must be positive.");
            if (entry.AdditiveBonusXp < 0)
                AddError(bag, "streakCurve", "AdditiveBonusXp cannot be negative.");
            expectedDay++;
        }
    }

    private static void ValidateSeasonalBoosts(CreatePolicyVersionRequest request, Dictionary<string, List<string>> bag) {
        if (request.SeasonalBoosts is null)
            return;

        List<SeasonalBoost> boosts = request.SeasonalBoosts;
        for (int i = 0; i < boosts.Count; i++) {
            SeasonalBoost boost = boosts[i];
            if (boost.Multiplier is < 0.1m or > 5.0m)
                AddError(bag, $"seasonalBoosts[{i}].multiplier", "Multiplier must be between 0.1 and 5.0.");
            if (boost.StartUtc >= boost.EndUtc)
                AddError(bag, $"seasonalBoosts[{i}].startUtc", "startUtc must be earlier than endUtc.");
        }

        List<(DateTime Start, DateTime End)> ordered = new(boosts.Count);
        foreach (SeasonalBoost boost in boosts)
            ordered.Add((boost.StartUtc, boost.EndUtc));

        ordered.Sort((a, b) => a.Start.CompareTo(b.Start));

        for (int i = 1; i < ordered.Count; i++) {
            if (ordered[i].Start < ordered[i - 1].End) {
                AddError(bag, "seasonalBoosts", "Seasonal boost windows may not overlap.");
                break;
            }
        }
    }

    private static void ValidateEffectiveAt(DateTime? effectiveAt, string fieldName, Dictionary<string, List<string>> bag) {
        if (!effectiveAt.HasValue)
            return;

        DateTime threshold = DateTime.UtcNow - PublishBackdateTolerance;
        if (effectiveAt.Value < threshold)
            AddError(bag, fieldName, $"{fieldName} cannot be in the past.");
    }

    private static void ValidateSegmentOverrides(
        IReadOnlyDictionary<string, int> overrides,
        Dictionary<string, List<string>> bag) {

        foreach ((string segment, int version) in overrides) {
            if (!SegmentKeyRegex.IsMatch(segment))
                AddError(bag, $"overrides.{segment}", "Segment keys must be alphanumeric/underscore up to 32 characters.");
            if (version <= 0)
                AddError(bag, $"overrides.{segment}", "Policy version numbers must be positive.");
        }
    }

    private static void ValidatePolicyKey(string policyKey, Dictionary<string, List<string>> bag) {
        if (string.IsNullOrWhiteSpace(policyKey))
            AddError(bag, "policyKey", "policyKey is required.");
        else if (!PolicyKeyRegex.IsMatch(policyKey))
            AddError(bag, "policyKey", "policyKey must be lowercase letters, digits, '-' or '_', length 3-64.");
    }

    private static void ValidatePositiveVersion(int policyVersion, Dictionary<string, List<string>> bag) {
        if (policyVersion <= 0)
            AddError(bag, "policyVersion", "policyVersion must be positive.");
    }

    private static void AddError(Dictionary<string, List<string>> bag, string field, string message) {
        if (!bag.TryGetValue(field, out List<string>? list)) {
            list = [];
            bag[field] = list;
        }

        list.Add(message);
    }

    private static bool BuildResult(
        Dictionary<string, List<string>> bag,
        out IDictionary<string, string[]>? errors) {

        if (bag.Count == 0) {
            errors = null;
            return true;
        }

        Dictionary<string, string[]> mapped = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, List<string>> entry in bag)
            mapped[entry.Key] = [.. entry.Value];

        errors = mapped;

        return false;
    }
}
