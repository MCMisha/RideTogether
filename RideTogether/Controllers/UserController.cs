using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideTogether.Entities.DTO;
using RideTogether.Interfaces;

namespace RideTogether.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _userService.Login(loginDto.Username, loginDto.Password);

        if (user is null)
            return Unauthorized(new { message = "Username or password is incorrect" });

        const string scheme = CookieAuthenticationDefaults.AuthenticationScheme;

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
            scheme);

        await HttpContext.SignInAsync(scheme, new ClaimsPrincipal(identity));

        return Ok(user);
    }
    
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
    
    // Add registration
}