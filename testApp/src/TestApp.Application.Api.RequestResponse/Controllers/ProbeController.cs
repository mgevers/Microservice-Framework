using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TestApp.Application.Api.RequestResponse.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProbeController : ControllerBase
{
    [HttpGet("public")]
    public async Task<IActionResult> PublicEndpoint()
    {
        var claims = User.Claims
            .Select(claim => new ClaimView(claim.Type, claim.Value))
            .ToList();

        var result = new OkObjectResult(new ProbeResponse(claims));

        return await Task.FromResult(result);
    }

    [Authorize]
    [HttpGet("private")]
    public async Task<IActionResult> PrivateEndpoint()
    {
        var claims = User.Claims
            .Select(claim => new ClaimView(claim.Type, claim.Value))
            .ToList();

        var result = new OkObjectResult(new ProbeResponse(claims));

        return await Task.FromResult(result);
    }
}

public record ClaimView(string ClaimKey, string ClaimValue) { }

public record ProbeResponse(List<ClaimView> Claims) { }
