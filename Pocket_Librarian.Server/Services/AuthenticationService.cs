using System.Security.Cryptography;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;

namespace Pocket_Librarian.Server.Services;

public class AuthenticationService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private readonly AuthenticationRepository _repository;

    public AuthenticationService(AuthenticationRepository repository)
    {
        _repository = repository;
    }

    public (bool Succeeded, string Message, AuthenticatedUser? User) Register(
        RegisterRequest request)
    {
        string firstName = request.FirstName.Trim();
        string lastName = request.LastName.Trim();
        string email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(firstName)
            || string.IsNullOrWhiteSpace(lastName)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, "First name, last name, email, and password are required.", null);
        }

        if (!email.Contains('@'))
        {
            return (false, "A valid email address is required.", null);
        }

        if (request.Password.Length < 6)
        {
            return (false, "Password must be at least six characters.", null);
        }

        if (_repository.EmailExists(email))
        {
            return (false, "An account with that email already exists.", null);
        }

        // Owen's PBKDF2 approach is retained. The salt and hash are stored together
        // because Jack's Users table has one PasswordHash column and no salt column.
        string passwordHash = HashPassword(request.Password);
        int userId = _repository.CreateUser(firstName, lastName, email, passwordHash);

        return (
            true,
            "Registration successful. Please sign in.",
            new AuthenticatedUser(userId, firstName, lastName, email, "Member"));
    }

    public AuthenticatedUser? Authenticate(string email, string password)
    {
        StoredUser? storedUser = _repository.FindByEmail(email.Trim().ToLowerInvariant());

        if (storedUser is null || !VerifyPassword(password, storedUser.PasswordHash))
        {
            return null;
        }

        // Jack's database constraint supports Member and Staff only.
        if (storedUser.Role is not ("Member" or "Staff"))
        {
            return null;
        }

        return new AuthenticatedUser(
            storedUser.UserId,
            storedUser.FirstName,
            storedUser.LastName,
            storedUser.Email,
            storedUser.Role);
    }

    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedValue)
    {
        string[] parts = storedValue.Split('.', 2);

        // Jack's seed data contains mock hashes, so malformed legacy values simply
        // fail authentication instead of crashing the API.
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
