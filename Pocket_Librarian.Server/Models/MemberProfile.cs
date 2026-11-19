namespace Pocket_Librarian.Server.Models;

// Represents the profile information returned to the website
public class MemberProfile
{
    // The member's database ID
    public int MemberId { get; set; }

    // The member's first name
    public string FirstName { get; set; } = "";

    // The member's last name
    public string LastName { get; set; } = "";

    // The member's email address
    public string Email { get; set; } = "";

    // The member's username
    public string Username { get; set; } = "";

    // The member's role, such as Member, Staff, or Administrator
    public string Role { get; set; } = "";
}