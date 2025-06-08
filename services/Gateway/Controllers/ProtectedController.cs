using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Route("gateway-test")]
public class ProtectedController : ControllerBase
{
    [HttpGet("ping")]
    [Authorize]
    public IActionResult Get()
    {
        var username = User.FindFirst("username")?.Value;
        return Ok(new { message = $"🎉 Authenticated as {username}" });
    }
}
