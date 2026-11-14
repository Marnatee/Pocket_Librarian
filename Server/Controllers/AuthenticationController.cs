using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Pocket_Librarian.BLL.Models;
using ProjectAuthenticationService = Pocket_Librarian.BLL.Services.AuthenticationService;

namespace Pocket_Librarian.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly ProjectAuthenticationService _authenticationService;

    public AuthenticationController(ProjectAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("register")]
    public ActionResult Register(RegisterRequest request)
    {
        try
        {
            var result = _authenticationService.Register(request);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = result.Message });
            }

            return StatusCode(StatusCodes.Status201Created, new { message = result.Message });
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The account could not be created because the database is unavailable." });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticatedUser>> Login(LoginRequest request)
    {
        try
        {
            AuthenticatedUser? user = _authenticationService.Authenticate(
                request.Email,
                request.Password);

            if (user is null)
            {
                return Unauthorized(new { message = "Email or password is incorrect." });
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            ClaimsIdentity identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Ok(user);
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The login service could not reach the database." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult GetCurrentUser()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            firstName = User.FindFirstValue(ClaimTypes.GivenName),
            lastName = User.FindFirstValue(ClaimTypes.Surname),
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
