using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Authentication;


public record UserLogin(string Username, string Password);
public record RefreshTokenRequest(string RefreshToken);

[ApiController]
[Route("api/authentication")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpPost("register")]
    public IActionResult Register([FromBody] UserLogin user)
    {
        var authResponse = accountService.Register(user.Username, user.Password);
        if (authResponse.IsSuccessful)
        {
            return Ok(new { message = authResponse.Message });
        }
        return BadRequest(new { message = authResponse.Message });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] UserLogin user)
    {
        var authResponse = accountService.Login(user.Username, user.Password);
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
