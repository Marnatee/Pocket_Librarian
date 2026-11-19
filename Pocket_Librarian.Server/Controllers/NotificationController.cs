using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;

namespace Pocket_Librarian.Server.Controllers;

// Identifies this class as an API controller
[ApiController]

// Sets the base URL for notification requests
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly NotificationRepository _repository;

    public NotificationController(
        NotificationRepository repository)
    {
        _repository = repository;
    }

    // Retrieves all notifications for a member
    //
    // Example:
    // GET /api/notifications/GregBooks
    [HttpGet("{username}")]
    public async Task<ActionResult<List<Notification>>>
        GetNotifications(string username)
    {
        List<Notification> notifications =
            await _repository.GetNotificationsAsync(
                username);

        return Ok(notifications);
    }

    // Creates a notification for a member
    //
    // Example:
    // POST /api/notifications/GregBooks
    [HttpPost("{username}")]
    public async Task<IActionResult> CreateNotification(
        string username,
        [FromBody]
        CreateNotificationRequest request)
    {
        bool created =
            await _repository.CreateNotificationAsync(
                username,
                request);

        if (!created)
        {
            return NotFound(
                "User was not found.");
        }

        return Ok(
            "Notification created successfully.");
    }

    // Marks a notification as read
    //
    // Example:
    // PUT /api/notifications/GregBooks/1/read
    [HttpPut("{username}/{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(
        string username,
        int notificationId)
    {
        bool updated =
            await _repository.MarkAsReadAsync(
                username,
                notificationId);

        if (!updated)
        {
            return NotFound(
                "Notification was not found.");
        }

        return Ok(
            "Notification marked as read.");
    }
}