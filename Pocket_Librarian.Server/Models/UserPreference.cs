namespace Pocket_Librarian.Server.Models;

// Represents the preferences returned to the website
public class UserPreferences
{
    // ID of the user who owns these preferences
    public int UserId { get; set; }

    // Username associated with the preferences
    public string Username { get; set; } = "";

    // Selected language, such as "en" or "es"
    public string Language { get; set; } = "en";

    // Selected website theme, such as "light" or "dark"
    public string Theme { get; set; } = "light";

    // Selected text size, such as "small", "medium", or "large"
    public string FontSize { get; set; } = "medium";
}