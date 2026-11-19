using System.ComponentModel.DataAnnotations;

namespace Pocket_Librarian.Server.Models;

// Represents the information needed to send a message
public class CreateMessageRequest
{
    // Message subject
    [Required]
    public string Subject { get; set; } = "";

    // Message body
    [Required]
    public string MessageText { get; set; } = "";
}