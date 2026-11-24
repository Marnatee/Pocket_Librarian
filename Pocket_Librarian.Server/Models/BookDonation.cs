namespace Pocket_Librarian.Server.Models;

// Represents a book donation stored in the database.
public class BookDonation
{
    public int DonationId { get; set; }
    public int UserId { get; set; }

    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string? ISBN { get; set; }
    public string Genre { get; set; } = "";
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string BookCondition { get; set; } = "";
    public int NumberOfCopies { get; set; }

    // Optional fields
    public string? CoverImageUrl { get; set; }
    public string? DonationComments { get; set; }

    // Every new donation begins in this state.
    public string Status { get; set; } = "Pending Verification";

    public DateTime SubmittedAt { get; set; }
}