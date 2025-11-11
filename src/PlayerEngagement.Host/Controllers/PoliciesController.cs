using Microsoft.AspNetCore.Mvc;

namespace PlayerEngagement.Host.Controllers;

[ApiController]
[Route("xp/policies")]
public sealed class PoliciesController : ControllerBase {
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { message = "Policy API ready" });
}
