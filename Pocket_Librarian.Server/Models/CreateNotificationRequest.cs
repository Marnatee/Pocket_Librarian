using System.ComponentModel.DataAnnotations;

namespace Pocket_Librarian.Server.Models;

// Represents the information needed to create
// a notification for a member
public class CreateNotificationRequest
{
    [Required]
    public string Title { get; set; } = "";

    [Required]
    public string MessageText { get; set; } = "";
}