using ApiAggregation.Authentication.Abstractions;
using ApiAggregation.Authentication.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Authentication.Controllers;

[ApiController]
[Route("api/authentication")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpPost("register")]
    public IActionResult Register([FromBody] AuthRequest request)
    {
        var authResponse = accountService.Register(request);
        if (authResponse.IsSuccessful)
        {
            return Ok(new { message = authResponse.Message });
        }
        return BadRequest(new { message = authResponse.Message });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AuthRequest request)
    {
        var authResponse = accountService.Login(request);
        if (authResponse.IsSuccessful)
        {
            return Ok(authResponse);
        }
        return BadRequest(new { message = authResponse.Message });
    }
    
    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshTokenRequest request)
    {
        var authResponse = accountService.RefreshToken(request.RefreshToken);
        if (authResponse.IsSuccessful)
        {
            return Ok(authResponse);
        }
        return BadRequest(new { message = authResponse.Message });
    }
}
