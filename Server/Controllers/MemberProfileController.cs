using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.BLL.Services;

namespace Pocket_Librarian.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/member-profile")]
public sealed class MemberProfileController : ControllerBase
{
    private readonly MemberProfileService _service;

    public MemberProfileController(MemberProfileService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<ActionResult<MemberProfile>> GetCurrentProfile()
    {
        try
        {
            int userId = GetCurrentUserId();
            MemberProfile? profile =
                await _service.GetByUserIdAsync(userId);

            return profile is null
                ? NotFound(new { message = "Member profile was not found." })
                : Ok(profile);
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The profile service could not reach the database." });
        }
        catch (ArgumentException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPut("me")]
    public async Task<ActionResult<MemberProfile>> UpdateCurrentProfile(
        [FromBody] UpdateMemberProfileRequest request)
    {
        try
        {
            int userId = GetCurrentUserId();
            var result = await _service.UpdateAsync(userId, request);

            if (!result.Succeeded)
            {
                return result.Message.Contains("already in use", StringComparison.Ordinal)
                    ? Conflict(new { message = result.Message })
                    : BadRequest(new { message = result.Message });
            }

            if (result.Profile is null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "The updated member profile could not be loaded." });
            }

            await RefreshAuthenticationCookieAsync(result.Profile);
            return Ok(result.Profile);
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The profile service could not reach the database." });
        }
        catch (ArgumentException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int userId) && userId > 0
            ? userId
            : throw new ArgumentException("The signed-in user could not be identified.");
    }

    private async Task RefreshAuthenticationCookieAsync(MemberProfile profile)
    {
        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, profile.UserId.ToString()),
            new Claim(ClaimTypes.Name, profile.Email),
            new Claim(ClaimTypes.Email, profile.Email),
            new Claim(ClaimTypes.GivenName, profile.FirstName),
            new Claim(ClaimTypes.Surname, profile.LastName),
            new Claim(ClaimTypes.Role, profile.Role)
        };

        ClaimsIdentity identity = new(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
