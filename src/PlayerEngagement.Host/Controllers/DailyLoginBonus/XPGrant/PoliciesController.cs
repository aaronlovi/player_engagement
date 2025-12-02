using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using PlayerEngagement.Host.Contracts.Policies.DailyLoginBonus.XPGrant;
using PlayerEngagement.Host.Validation.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Policies.Services.DailyLoginBonus.XPGrant;

namespace PlayerEngagement.Host.Controllers.DailyLoginBonus.XPGrant;

/// <summary>HTTP API controller exposing policy CRUD endpoints.</summary>
[ApiController]
[Route("daily-login-bonus/xp-grant/policies")]
[Produces("application/json")]
public sealed class PoliciesController : ControllerBase {
    private const string DefaultCreatedBy = "api";

    private readonly ILogger<PoliciesController> _logger;
    private readonly IPolicyDocumentPersistenceService _policyPersistence;

    /// <summary>Initializes a new instance of the <see cref="PoliciesController"/>.</summary>
    public PoliciesController(
        ILogger<PoliciesController> logger,
        IPolicyDocumentPersistenceService policyPersistence) {
        _logger = logger;
        _policyPersistence = policyPersistence ?? throw new ArgumentNullException(nameof(policyPersistence));
    }

    /// <summary>Simple liveness endpoint for the policy API.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { message = "Policy API ready" });

    /// <summary>Create a draft policy version for the given policy key.</summary>
    [HttpPost("{policyKey}/versions")]
    public Task<IActionResult> CreatePolicyVersionAsync(
        string policyKey,
        [FromBody] CreatePolicyVersionRequest request,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateCreate(policyKey, request, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation("CreatePolicyVersion called for {PolicyKey}", policyKey);

        PolicyVersionWriteDto dto = BuildPolicyWriteDto(policyKey, request);
        List<PolicyStreakCurveEntryDTO> streakDtos = BuildStreakCurveDtos(policyKey, request.StreakCurve);
        List<PolicySeasonalBoostDTO> boostDtos = BuildSeasonalBoostDtos(policyKey, request.SeasonalBoosts);

        return CreateDraftInternalAsync(dto, streakDtos, boostDtos, ct);
    }

    /// <summary>Publish a draft/archived policy version and optionally schedule effectiveness.</summary>
    [HttpPost("{policyKey}/versions/{policyVersion:long}/publish")]
    public Task<IActionResult> PublishPolicyVersionAsync(
        string policyKey,
        long policyVersion,
        [FromBody] PublishPolicyVersionRequest request,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidatePublish(policyKey, policyVersion, request, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation("PublishPolicyVersion called for {PolicyKey} v{PolicyVersion}", policyKey, policyVersion);

        List<PolicySegmentOverrideDTO> overrides = BuildSegmentOverrideDtos(
            policyKey,
            request.SegmentOverrides ?? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase),
            request.EffectiveAt ?? DateTime.UtcNow);

        return PublishInternalAsync(policyKey, policyVersion, request.EffectiveAt, overrides, ct);
    }

    /// <summary>Retire a published policy version.</summary>
    [HttpPost("{policyKey}/versions/{policyVersion:long}/retire")]
    public Task<IActionResult> RetirePolicyVersionAsync(
        string policyKey,
        long policyVersion,
        [FromBody] RetirePolicyVersionRequest request,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateRetire(policyKey, policyVersion, request, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation("RetirePolicyVersion called for {PolicyKey} v{PolicyVersion}", policyKey, policyVersion);
        return RetireInternalAsync(policyKey, policyVersion, request.RetiredAt ?? DateTime.UtcNow, ct);
    }

    /// <summary>Retrieve a specific policy version by key and version number.</summary>
    [HttpGet("{policyKey}/versions/{policyVersion:long}")]
    public async Task<IActionResult> GetPolicyVersionAsync(
        string policyKey,
        long policyVersion,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateVersionLookup(policyKey, policyVersion, out IDictionary<string, string[]>? errors))
            return ValidationProblem(CreateValidationProblem(errors!));

        PolicyDocument? document = await _policyPersistence.GetPolicyVersionAsync(policyKey, policyVersion, ct);
        if (document is null)
            return NotFound(new { message = $"Policy {policyKey} version {policyVersion} was not found." });

        _logger.LogInformation("GetPolicyVersion returned {PolicyKey} v{PolicyVersion}", policyKey, policyVersion);
        return Ok(document);
    }

    /// <summary>List policy versions for a key with optional filtering.</summary>
    [HttpGet("{policyKey}/versions")]
    public Task<IActionResult> ListPolicyVersionsAsync(
        string policyKey,
        [FromQuery] string? status,
        [FromQuery] DateTime? effectiveBefore,
        [FromQuery] int? limit,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateListQuery(policyKey, status, effectiveBefore, limit, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation(
            "ListPolicyVersions called for {PolicyKey} (status={Status}, effectiveBefore={EffectiveBefore}, limit={Limit})",
            policyKey,
            status,
            effectiveBefore,
            limit);

        return ListVersionsInternalAsync(policyKey, status, effectiveBefore, limit, ct);
    }

    /// <summary>Get the currently active policy document for a key.</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActivePolicyAsync(
        [FromQuery] string policyKey,
        [FromQuery] string? segment,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateActiveQuery(policyKey, segment, out IDictionary<string, string[]>? errors))
            return ValidationProblem(CreateValidationProblem(errors!));

        PolicyDocument? document = await _policyPersistence.GetCurrentPolicyAsync(policyKey, DateTime.UtcNow, ct);
        if (document is null)
            return NotFound(new { message = $"Active policy for key '{policyKey}' was not found." });

        _logger.LogInformation("GetActivePolicy called for {PolicyKey} (segment={Segment})", policyKey, segment);
        return Ok(document);
    }

    /// <summary>Get segment override mappings for a policy.</summary>
    [HttpGet("{policyKey}/segments")]
    public async Task<IActionResult> GetSegmentOverridesAsync(string policyKey, CancellationToken ct) {
        if (!PolicyRequestValidator.TryValidateSegmentOverrides(policyKey, out IDictionary<string, string[]>? errors))
            return ValidationProblem(CreateValidationProblem(errors!));

        IReadOnlyDictionary<string, long> overrides = await _policyPersistence.GetSegmentOverridesAsync(policyKey, ct);
        _logger.LogInformation("GetSegmentOverrides called for {PolicyKey}", policyKey);
        return Ok(overrides);
    }

    /// <summary>Replace segment override mappings for a policy.</summary>
    [HttpPut("{policyKey}/segments")]
    public Task<IActionResult> UpdateSegmentOverridesAsync(
        string policyKey,
        [FromBody] UpdateSegmentOverridesRequest request,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateSegmentOverrideUpdate(policyKey, request, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation("UpdateSegmentOverrides called for {PolicyKey}", policyKey);
        List<PolicySegmentOverrideDTO> overrides = BuildSegmentOverrideDtos(policyKey, request.Overrides, DateTime.UtcNow);
        return UpdateSegmentOverridesInternalAsync(policyKey, overrides, ct);
    }

    private static ValidationProblemDetails CreateValidationProblem(IDictionary<string, string[]> errors) =>
        new(errors) {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed."
        };

    private static PolicyVersionWriteDto BuildPolicyWriteDto(string policyKey, CreatePolicyVersionRequest request) {
        string streakParameters = JsonSerializer.Serialize(request.StreakModelParameters);

        return new PolicyVersionWriteDto(
            policyKey,
            request.DisplayName,
            request.Description,
            request.BaseXpAmount,
            request.Currency,
            request.ClaimWindowStartMinutes,
            request.ClaimWindowDurationHours,
            request.AnchorStrategy,
            request.GraceAllowedMisses,
            request.GraceWindowDays,
            request.StreakModelType,
            streakParameters,
            request.PreviewSampleWindowDays,
            request.PreviewDefaultSegment,
            "{}",
            request.EffectiveAt,
            DateTime.UtcNow,
            DefaultCreatedBy,
            0,
            null);
    }

    private static List<PolicyStreakCurveEntryDTO> BuildStreakCurveDtos(string policyKey, IReadOnlyList<StreakCurveEntry> streakCurve) {
        List<PolicyStreakCurveEntryDTO> dtos = new(streakCurve.Count);
        foreach (StreakCurveEntry entry in streakCurve) {
            dtos.Add(new PolicyStreakCurveEntryDTO(
                0,
                policyKey,
                0,
                entry.DayIndex,
                entry.Multiplier,
                entry.AdditiveBonusXp,
                entry.CapNextDay));
        }

        return dtos;
    }

    private static List<PolicySeasonalBoostDTO> BuildSeasonalBoostDtos(string policyKey, IReadOnlyList<SeasonalBoost> boosts) {
        List<PolicySeasonalBoostDTO> dtos = new(boosts.Count);
        foreach (SeasonalBoost boost in boosts) {
            dtos.Add(new PolicySeasonalBoostDTO(
                0,
                policyKey,
                0,
                boost.Label,
                boost.Multiplier,
                boost.StartUtc,
                boost.EndUtc));
        }

        return dtos;
    }

    private static List<PolicySegmentOverrideDTO> BuildSegmentOverrideDtos(
        string policyKey,
        IReadOnlyDictionary<string, long> overrides,
        DateTime createdAt) {

        List<PolicySegmentOverrideDTO> dtos = new(overrides.Count);
        foreach (KeyValuePair<string, long> pair in overrides)
            dtos.Add(new PolicySegmentOverrideDTO(0, pair.Key, policyKey, pair.Value, createdAt, DefaultCreatedBy));

        return dtos;
    }

    private IActionResult MapFailure(InnoAndLogic.Shared.IResult result, string defaultMessage) {
        int status = result.ErrorCode switch {
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Duplicate => StatusCodes.Status409Conflict,
            ErrorCodes.ValidationError => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        string message = !string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.ErrorMessage! : defaultMessage;
        return Problem(statusCode: status, detail: message);
    }

    private async Task<IActionResult> CreateDraftInternalAsync(
        PolicyVersionWriteDto dto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streak,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct) {

        Result<PolicyDocument> result = await _policyPersistence.CreatePolicyDraftAsync(dto, streak, boosts, ct);
        if (result.IsFailure || result.Value is null)
            return MapFailure(result, "Failed to create policy draft.");

        string location = Url.ActionLink(nameof(GetPolicyVersionAsync), values: new { policyKey = dto.PolicyKey, policyVersion = result.Value.Version.PolicyVersion }) ?? string.Empty;
        return Created(location, result.Value);
    }

    private async Task<IActionResult> PublishInternalAsync(
        string policyKey,
        long policyVersion,
        DateTime? effectiveAt,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides,
        CancellationToken ct) {

        Result<PolicyDocument> result = await _policyPersistence.PublishPolicyVersionAsync(
            policyKey,
            policyVersion,
            DateTime.UtcNow,
            effectiveAt,
            overrides,
            ct);

        if (result.IsFailure || result.Value is null)
            return MapFailure(result, "Failed to publish policy version.");

        string location = Url.ActionLink(nameof(GetPolicyVersionAsync), values: new { policyKey, policyVersion }) ?? string.Empty;
        return Accepted(location, result.Value);
    }

    private async Task<IActionResult> RetireInternalAsync(string policyKey, long policyVersion, DateTime retiredAt, CancellationToken ct) {
        Result<PolicyVersionDocument> result = await _policyPersistence.RetirePolicyVersionAsync(policyKey, policyVersion, retiredAt, ct);
        if (result.IsFailure || result.Value is null)
            return MapFailure(result, "Failed to retire policy version.");

        return Ok(result.Value);
    }

    private async Task<IActionResult> ListVersionsInternalAsync(string policyKey, string? status, DateTime? effectiveBefore, int? limit, CancellationToken ct) {
        IReadOnlyList<PolicyVersionDocument> versions = await _policyPersistence.ListPolicyVersionsAsync(policyKey, status, effectiveBefore, limit, ct);
        return Ok(versions);
    }

    private async Task<IActionResult> UpdateSegmentOverridesInternalAsync(
        string policyKey,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides,
        CancellationToken ct) {

        Result<IReadOnlyDictionary<string, long>> result = await _policyPersistence.UpdateSegmentOverridesAsync(policyKey, overrides, ct);
        if (result.IsFailure || result.Value is null)
            return MapFailure(result, "Failed to update segment overrides.");

        return Ok(result.Value);
    }
}
