using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.BLL.Services;

namespace Pocket_Librarian.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationController : ControllerBase
{
    private readonly NotificationService _service;

    public NotificationController(NotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<Notification>>> GetCurrentNotifications()
    {
        try
        {
            int userId = GetCurrentUserId();
            return Ok(await _service.GetForUserAsync(userId));
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The notification service could not reach the database." });
        }
        catch (ArgumentException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPut("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        try
        {
            int userId = GetCurrentUserId();
            bool updated = await _service.MarkAsReadAsync(
                userId,
                notificationId);

            return updated
                ? NoContent()
                : NotFound(new { message = "Notification was not found." });
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The notification service could not reach the database." });
        }
        catch (ArgumentException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Staff")]
    [HttpPost("user/{userId:int}")]
    public async Task<IActionResult> CreateForUser(
        int userId,
        [FromBody] CreateNotificationRequest request)
    {
        try
        {
            bool created = await _service.CreateForUserAsync(
                userId,
                request);

            return created
                ? Ok(new { message = "Notification created successfully." })
                : NotFound(new { message = "User was not found." });
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The notification service could not reach the database." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int userId) && userId > 0
            ? userId
            : throw new ArgumentException("The signed-in user could not be identified.");
    }
}
