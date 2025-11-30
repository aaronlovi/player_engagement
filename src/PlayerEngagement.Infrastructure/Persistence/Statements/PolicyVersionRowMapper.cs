using System;
using System.Data.Common;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence.Statements;

internal static class PolicyVersionRowMapper {
    private static readonly object OrdinalLock = new();

    private static int _policyIdIndex = -1;
    private static int _policyKeyIndex = -1;
    private static int _displayNameIndex = -1;
    private static int _descriptionIndex = -1;
    private static int _policyVersionIndex = -1;
    private static int _statusIndex = -1;
    private static int _baseXpIndex = -1;
    private static int _currencyIndex = -1;
    private static int _claimStartIndex = -1;
    private static int _claimDurationIndex = -1;
    private static int _anchorStrategyIndex = -1;
    private static int _graceAllowedIndex = -1;
    private static int _graceWindowIndex = -1;
    private static int _streakModelTypeIndex = -1;
    private static int _streakModelParamsIndex = -1;
    private static int _previewSampleIndex = -1;
    private static int _previewSegmentIndex = -1;
    private static int _seasonalMetadataIndex = -1;
    private static int _effectiveAtIndex = -1;
    private static int _supersededAtIndex = -1;
    private static int _createdAtIndex = -1;
    private static int _createdByIndex = -1;
    private static int _publishedAtIndex = -1;

    internal static void EnsureOrdinals(DbDataReader reader) {
        if (_policyIdIndex != -1)
            return;

        lock (OrdinalLock) {
            if (_policyIdIndex != -1)
                return;

            _policyIdIndex = reader.GetOrdinal("policy_id");
            _policyKeyIndex = reader.GetOrdinal("policy_key");
            _displayNameIndex = reader.GetOrdinal("display_name");
            _descriptionIndex = reader.GetOrdinal("description");
            _policyVersionIndex = reader.GetOrdinal("policy_version");
            _statusIndex = reader.GetOrdinal("status");
            _baseXpIndex = reader.GetOrdinal("base_xp_amount");
            _currencyIndex = reader.GetOrdinal("currency");
            _claimStartIndex = reader.GetOrdinal("claim_window_start_minutes");
            _claimDurationIndex = reader.GetOrdinal("claim_window_duration_hours");
            _anchorStrategyIndex = reader.GetOrdinal("anchor_strategy");
            _graceAllowedIndex = reader.GetOrdinal("grace_allowed_misses");
            _graceWindowIndex = reader.GetOrdinal("grace_window_days");
            _streakModelTypeIndex = reader.GetOrdinal("streak_model_type");
            _streakModelParamsIndex = reader.GetOrdinal("streak_model_parameters");
            _previewSampleIndex = reader.GetOrdinal("preview_sample_window_days");
            _previewSegmentIndex = reader.GetOrdinal("preview_default_segment");
            _seasonalMetadataIndex = reader.GetOrdinal("seasonal_metadata");
            _effectiveAtIndex = reader.GetOrdinal("effective_at");
            _supersededAtIndex = reader.GetOrdinal("superseded_at");
            _createdAtIndex = reader.GetOrdinal("created_at");
            _createdByIndex = reader.GetOrdinal("created_by");
            _publishedAtIndex = reader.GetOrdinal("published_at");
        }
    }

    internal static PolicyVersionDTO ReadPolicyVersion(DbDataReader reader) {
        EnsureOrdinals(reader);

        return new PolicyVersionDTO(
            reader.GetInt64(_policyIdIndex),
            reader.GetString(_policyKeyIndex),
            reader.GetString(_displayNameIndex),
            reader.GetString(_descriptionIndex),
            reader.GetInt64(_policyVersionIndex),
            reader.GetString(_statusIndex),
            reader.GetInt32(_baseXpIndex),
            reader.GetString(_currencyIndex),
            reader.GetInt32(_claimStartIndex),
            reader.GetInt32(_claimDurationIndex),
            reader.GetString(_anchorStrategyIndex),
            reader.GetInt32(_graceAllowedIndex),
            reader.GetInt32(_graceWindowIndex),
            reader.GetString(_streakModelTypeIndex),
            reader.GetString(_streakModelParamsIndex),
            reader.GetInt32(_previewSampleIndex),
            reader.GetString(_previewSegmentIndex),
            reader.GetString(_seasonalMetadataIndex),
            reader.IsDBNull(_effectiveAtIndex) ? null : reader.GetDateTime(_effectiveAtIndex),
            reader.IsDBNull(_supersededAtIndex) ? null : reader.GetDateTime(_supersededAtIndex),
            reader.GetDateTime(_createdAtIndex),
            reader.GetString(_createdByIndex),
            reader.IsDBNull(_publishedAtIndex) ? null : reader.GetDateTime(_publishedAtIndex));
    }

    internal static ActivePolicyDTO ToActivePolicy(PolicyVersionDTO source) =>
        new(
            source.PolicyId,
            source.PolicyKey,
            source.DisplayName,
            source.Description,
            source.PolicyVersion,
            source.Status,
            source.BaseXpAmount,
            source.Currency,
            source.ClaimWindowStartMinutes,
            source.ClaimWindowDurationHours,
            source.AnchorStrategy,
            source.GraceAllowedMisses,
            source.GraceWindowDays,
            source.StreakModelType,
            source.StreakModelParameters,
            source.PreviewSampleWindowDays,
            source.PreviewDefaultSegment,
            source.SeasonalMetadata,
            source.EffectiveAt,
            source.SupersededAt,
            source.CreatedAt,
            source.CreatedBy,
            source.PublishedAt);
}
