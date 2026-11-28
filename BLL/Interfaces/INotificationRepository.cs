using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetForUserAsync(int userId);

    Task<bool> CreateForUserAsync(
        int userId,
        CreateNotificationRequest request);

    Task<bool> MarkAsReadAsync(
        int userId,
        int notificationId);

    Task<bool> DeleteForUserAsync(
        int userId,
        int notificationId);
}
