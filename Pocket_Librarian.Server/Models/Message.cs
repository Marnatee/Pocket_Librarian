namespace Pocket_Librarian.Server.Models;

// Represents one message in a member's inbox
public class Message
{
    // Unique ID for the message
    public int MessageId { get; set; }

    // Username of the sender
    public string Sender { get; set; } = "";

    // Subject of the message
    public string Subject { get; set; } = "";

    // Main message content
    public string MessageText { get; set; } = "";

    // Date and time the message was sent
    public DateTime SentDate { get; set; }

    // Indicates whether the message has been read
    public bool IsRead { get; set; }
}