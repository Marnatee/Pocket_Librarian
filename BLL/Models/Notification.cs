namespace Pocket_Librarian.BLL.Models;

public sealed class Notification
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public bool IsRead { get; set; }
}

public sealed class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;
}
