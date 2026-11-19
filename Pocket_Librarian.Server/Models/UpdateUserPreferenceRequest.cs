using System.ComponentModel.DataAnnotations;

namespace Pocket_Librarian.Server.Models;

// Represents the preference values sent when
// a member changes their settings
public class UpdateUserPreferencesRequest
{
    // Language selected by the member
    [Required]
    public string Language { get; set; } = "en";

    // Theme selected by the member
    [Required]
    public string Theme { get; set; } = "light";

    // Font size selected by the member
    [Required]
    public string FontSize { get; set; } = "medium";
}