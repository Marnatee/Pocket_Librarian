using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public sealed class NotificationService
{
    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Notification>> GetForUserAsync(int userId)
    {
        ValidateUserId(userId);
        return _repository.GetForUserAsync(userId);
    }

    public Task<bool> MarkAsReadAsync(
        int userId,
        int notificationId)
    {
        ValidateUserId(userId);

        if (notificationId <= 0)
        {
            throw new ArgumentException(
                "Notification ID must be greater than zero.",
                nameof(notificationId));
        }

        return _repository.MarkAsReadAsync(
            userId,
            notificationId);
    }

    public Task<bool> CreateForUserAsync(
        int userId,
        CreateNotificationRequest request)
    {
        ValidateUserId(userId);

        string title = request.Title.Trim();
        string messageText = request.MessageText.Trim();

        if (string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(messageText))
        {
            throw new ArgumentException(
                "Notification title and message are required.");
        }

        return _repository.CreateForUserAsync(
            userId,
            new CreateNotificationRequest
            {
                Title = title,
                MessageText = messageText
            });
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "User ID must be greater than zero.",
                nameof(userId));
        }
    }
}
