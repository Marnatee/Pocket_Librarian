namespace Pocket_Librarian.Server.Models;

// Represents one notification sent to a member
public class Notification
{
    // Unique notification ID
    public int NotificationId { get; set; }

    // Notification title
    public string Title { get; set; } = "";

    // Main notification message
    public string MessageText { get; set; } = "";

    // Date and time the notification was created
    public DateTime CreatedDate { get; set; }

    // False means unread; true means read
    public bool IsRead { get; set; }
}