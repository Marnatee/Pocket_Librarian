namespace Pocket_Librarian.Server.Models;

public sealed class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record AuthenticatedUser(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role);
