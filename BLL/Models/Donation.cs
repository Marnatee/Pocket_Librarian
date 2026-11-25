namespace Pocket_Librarian.BLL.Models;

public sealed class Donation
{
    public int DonationId { get; set; }
    public int? BookId { get; set; }
    public int? SubmittedByUserId { get; set; }
    public int? ProcessedByStaffId { get; set; }
    public string? DonorName { get; set; }
    public string? DonorEmail { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? Genre { get; set; }
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? CoverImagePath { get; set; }
    public string SourceType { get; set; } = "Member donation";
    public string VerificationStatus { get; set; } = "Pending";
    public int CopiesDonated { get; set; } = 1;
    public string Condition { get; set; } = "Good";
    public string? DonationComments { get; set; }
    public string? StaffNotes { get; set; }
    public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;
    public DateTime? DateProcessed { get; set; }
    public DateTime? DateApproved { get; set; }
}
