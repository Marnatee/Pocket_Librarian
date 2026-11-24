namespace Pocket_Librarian.BLL.Models;

public sealed class BookDonationSubmission
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? Genre { get; set; }
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? BookCondition { get; set; }
    public int NumberOfCopies { get; set; } = 1;
    public byte[]? CoverImage { get; set; }
    public string? CoverImageContentType { get; set; }
    public string? CoverImageFileName { get; set; }
    public string? DonationComments { get; set; }
}

public sealed class BookDonation
{
    public int DonationId { get; set; }
    public int UserId { get; set; }
    public string? DonorName { get; set; }
    public string? DonorEmail { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? Genre { get; set; }
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? BookCondition { get; set; }
    public int NumberOfCopies { get; set; }
    public bool HasCoverImage { get; set; }
    public string? DonationComments { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
