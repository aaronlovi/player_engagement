using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Host.Contracts.Policies;
using PlayerEngagement.Host.Validation;
using PlayerEngagement.Infrastructure.Policies.Services;

namespace PlayerEngagement.Host.Controllers;

[ApiController]
[Route("xp/policies")]
[Produces("application/json")]
public sealed class PoliciesController : ControllerBase {
    private readonly ILogger<PoliciesController> _logger;
    private readonly IPolicyDocumentPersistenceService _policyPersistence;

    public PoliciesController(
        ILogger<PoliciesController> logger,
        IPolicyDocumentPersistenceService policyPersistence) {
        _logger = logger;
        _policyPersistence = policyPersistence ?? throw new ArgumentNullException(nameof(policyPersistence));
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { message = "Policy API ready" });

    [HttpPost("{policyKey}/versions")]
    public Task<IActionResult> CreatePolicyVersionAsync(
        string policyKey,
        [FromBody] CreatePolicyVersionRequest request,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateCreate(policyKey, request, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation("CreatePolicyVersion called for {PolicyKey}", policyKey);
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost("{policyKey}/versions/{policyVersion:int}/publish")]
    public Task<IActionResult> PublishPolicyVersionAsync(
        string policyKey,
        int policyVersion,
        [FromBody] PublishPolicyVersionRequest request,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidatePublish(policyKey, policyVersion, request, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation("PublishPolicyVersion called for {PolicyKey} v{PolicyVersion}", policyKey, policyVersion);
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpPost("{policyKey}/versions/{policyVersion:int}/retire")]
    public Task<IActionResult> RetirePolicyVersionAsync(
        string policyKey,
        int policyVersion,
        [FromBody] RetirePolicyVersionRequest request,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateRetire(policyKey, policyVersion, request, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation("RetirePolicyVersion called for {PolicyKey} v{PolicyVersion}", policyKey, policyVersion);
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    [HttpGet("{policyKey}/versions/{policyVersion:int}")]
    public async Task<IActionResult> GetPolicyVersionAsync(
        string policyKey,
        int policyVersion,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateVersionLookup(policyKey, policyVersion, out IDictionary<string, string[]>? errors))
            return ValidationProblem(CreateValidationProblem(errors!));

        PolicyDocument? document = await _policyPersistence.GetPolicyVersionAsync(policyKey, policyVersion, ct);
        if (document is null)
            return NotFound(new { message = $"Policy {policyKey} version {policyVersion} was not found." });

        _logger.LogInformation("GetPolicyVersion returned {PolicyKey} v{PolicyVersion}", policyKey, policyVersion);
        return Ok(document);
    }

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

        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

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

    [HttpGet("{policyKey}/segments")]
    public async Task<IActionResult> GetSegmentOverridesAsync(string policyKey, CancellationToken ct) {
        if (!PolicyRequestValidator.TryValidateSegmentOverrides(policyKey, out IDictionary<string, string[]>? errors))
            return ValidationProblem(CreateValidationProblem(errors!));

        IReadOnlyDictionary<string, int> overrides = await _policyPersistence.GetSegmentOverridesAsync(policyKey, ct);
        _logger.LogInformation("GetSegmentOverrides called for {PolicyKey}", policyKey);
        return Ok(overrides);
    }

    [HttpPut("{policyKey}/segments")]
    public Task<IActionResult> UpdateSegmentOverridesAsync(
        string policyKey,
        [FromBody] UpdateSegmentOverridesRequest request,
        CancellationToken ct) {

        if (!PolicyRequestValidator.TryValidateSegmentOverrideUpdate(policyKey, request, out IDictionary<string, string[]>? errors))
            return Task.FromResult<IActionResult>(ValidationProblem(CreateValidationProblem(errors!)));

        _logger.LogInformation("UpdateSegmentOverrides called for {PolicyKey}", policyKey);
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
    }

    private static ValidationProblemDetails CreateValidationProblem(IDictionary<string, string[]> errors) =>
        new(errors) {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed."
        };
}
