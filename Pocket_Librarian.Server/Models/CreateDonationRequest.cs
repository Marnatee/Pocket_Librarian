using System.ComponentModel.DataAnnotations;

namespace Pocket_Librarian.Server.Models;

// Contains the information submitted by a member.
public class CreateDonationRequest
{
    [Required]
    public string Title { get; set; } = "";

    [Required]
    public string Author { get; set; } = "";

    public string? ISBN { get; set; }

    [Required]
    public string Genre { get; set; } = "";

    public int? PublicationYear { get; set; }

    public string? Description { get; set; }

    [Required]
    public string BookCondition { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int NumberOfCopies { get; set; }

    public string? CoverImageUrl { get; set; }
    public string? DonationComments { get; set; }
}