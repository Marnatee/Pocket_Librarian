using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.DAL;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.DAL.Repositories;

public sealed class MemberProfileRepository : IMemberProfileRepository
{
    private readonly string _connectionString;

    public MemberProfileRepository(IConfiguration configuration)
    {
        _connectionString = configuration[
            "ConnectionStrings:PocketLibrarianDatabase"]
            ?? string.Empty;
    }

    public async Task<MemberProfile?> GetByUserIdAsync(int userId)
    {
        const string sql = """
            SELECT UserID, FirstName, LastName, Email, Role
            FROM dbo.Users
            WHERE UserID = @UserID;
            """;

        await using SqlConnection connection =
            new SqlConnection(DatabaseConnectionState.GetActiveConnectionString(_connectionString));
        await connection.OpenAsync();

        await using SqlCommand command =
            new SqlCommand(sql, connection);
        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new MemberProfile
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserID")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            Role = reader.GetString(reader.GetOrdinal("Role"))
        };
    }

    public async Task<bool> EmailExistsForOtherUserAsync(
        string email,
        int userId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Users
            WHERE Email = @Email
              AND UserID <> @UserID;
            """;

        await using SqlConnection connection =
            new SqlConnection(DatabaseConnectionState.GetActiveConnectionString(_connectionString));
        await connection.OpenAsync();

        await using SqlCommand command =
            new SqlCommand(sql, connection);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email;
        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    public async Task<bool> UpdateAsync(
        int userId,
        UpdateMemberProfileRequest request)
    {
        const string sql = """
            UPDATE dbo.Users
            SET FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email
            WHERE UserID = @UserID;
            """;

        await using SqlConnection connection =
            new SqlConnection(DatabaseConnectionState.GetActiveConnectionString(_connectionString));
        await connection.OpenAsync();

        await using SqlCommand command =
            new SqlCommand(sql, connection);
        command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 50).Value = request.FirstName;
        command.Parameters.Add("@LastName", SqlDbType.NVarChar, 50).Value = request.LastName;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = request.Email;
        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

        return await command.ExecuteNonQueryAsync() == 1;
    }
}
