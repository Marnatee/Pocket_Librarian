using Microsoft.Data.SqlClient;
using Pocket_Librarian.Server.Models;

namespace Pocket_Librarian.Server.Repositories;

// Handles database operations for member profiles
public class MemberProfileRepository
{
    // Provides access to appsettings.json
    private readonly IConfiguration _configuration;

    // Receives the configuration through dependency injection
    public MemberProfileRepository(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Retrieves one member's profile using their username
    public async Task<MemberProfile?> GetByUsernameAsync(
        string username)
    {
        // Retrieves the database connection string
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        // SQL query that retrieves the member's profile
        const string sql = @"
            SELECT
                UserID AS MemberId,
                FirstName,
                LastName,
                Email,
                Username,
                Role
            FROM dbo.RegisteredUsers
            WHERE Username = @Username;";

        // Opens a connection to SQL Server
        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        // Creates the SQL command
        await using var command =
            new SqlCommand(sql, connection);

        // Safely inserts the username into the query
        command.Parameters.AddWithValue(
            "@Username",
            username);

        // Executes the query and reads the result
        await using var reader =
            await command.ExecuteReaderAsync();

        // Returns null if no matching profile exists
        if (!await reader.ReadAsync())
        {
            return null;
        }

        // Converts the database row into a MemberProfile object
        return new MemberProfile
        {
            MemberId = Convert.ToInt32(
                reader["MemberId"]),

            FirstName =
                reader["FirstName"]?.ToString() ?? "",

            LastName =
                reader["LastName"]?.ToString() ?? "",

            Email =
                reader["Email"]?.ToString() ?? "",

            Username =
                reader["Username"]?.ToString() ?? "",

            Role =
                reader["Role"]?.ToString() ?? ""
        };
    }
}