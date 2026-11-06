using Microsoft.Data.SqlClient;
using System.Data;

namespace Pocket_Librarian.Server.Repositories;

public sealed record StoredUser(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string PasswordHash,
    string Role);

public class AuthenticationRepository
{
    private readonly string _connectionString;

    public AuthenticationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PocketLibrarianDatabase")
            ?? string.Empty;
    }

    public bool EmailExists(string email)
    {
        using SqlConnection connection = new SqlConnection(_connectionString);
        using SqlCommand command = new SqlCommand(
            "SELECT COUNT(1) FROM dbo.Users WHERE Email = @Email;",
            connection);

        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email;
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public int CreateUser(
        string firstName,
        string lastName,
        string email,
        string passwordHash)
    {
        const string sql = """
            INSERT INTO dbo.Users
                (FirstName, LastName, Email, PasswordHash, Role)
            OUTPUT INSERTED.UserID
            VALUES
                (@FirstName, @LastName, @Email, @PasswordHash, 'Member');
            """;

        using SqlConnection connection = new SqlConnection(_connectionString);
        using SqlCommand command = new SqlCommand(sql, connection);

        command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 50).Value = firstName;
        command.Parameters.Add("@LastName", SqlDbType.NVarChar, 50).Value = lastName;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email;
        command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 255).Value = passwordHash;

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public StoredUser? FindByEmail(string email)
    {
        const string sql = """
            SELECT UserID, FirstName, LastName, Email, PasswordHash, Role
            FROM dbo.Users
            WHERE Email = @Email;
            """;

        using SqlConnection connection = new SqlConnection(_connectionString);
        using SqlCommand command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email;

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new StoredUser(
            reader.GetInt32(reader.GetOrdinal("UserID")),
            reader.GetString(reader.GetOrdinal("FirstName")),
            reader.GetString(reader.GetOrdinal("LastName")),
            reader.GetString(reader.GetOrdinal("Email")),
            reader.GetString(reader.GetOrdinal("PasswordHash")),
            reader.GetString(reader.GetOrdinal("Role")));
    }
}
