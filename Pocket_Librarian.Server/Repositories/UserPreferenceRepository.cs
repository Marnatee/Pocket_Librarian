using Microsoft.Data.SqlClient;
using Pocket_Librarian.Server.Models;

namespace Pocket_Librarian.Server.Repositories;

// Handles database operations for user preferences
public class UserPreferencesRepository
{
    // Provides access to appsettings.json
    private readonly IConfiguration _configuration;

    // Receives configuration through dependency injection
    public UserPreferencesRepository(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Creates the UserPreferences table if it does not exist
    public async Task EnsurePreferencesTableAsync()
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        const string sql = @"
            IF OBJECT_ID('dbo.UserPreferences', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.UserPreferences
                (
                    PreferenceID INT IDENTITY(1,1)
                        PRIMARY KEY,

                    UserID INT NOT NULL,

                    Language NVARCHAR(10) NOT NULL
                        DEFAULT 'en',

                    Theme NVARCHAR(20) NOT NULL
                        DEFAULT 'light',

                    FontSize NVARCHAR(20) NOT NULL
                        DEFAULT 'medium',

                    FOREIGN KEY (UserID)
                        REFERENCES dbo.[Library Table](id)
                );
            END;";

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        await command.ExecuteNonQueryAsync();
    }

    // Retrieves a user's database ID using their username
    private async Task<int?> GetUserIdAsync(
        SqlConnection connection,
        string username)
    {
        const string sql = @"
            SELECT id
            FROM dbo.[Library Table]
            WHERE username = @Username;";

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Username",
            username);

        object? result =
            await command.ExecuteScalarAsync();

        if (result == null ||
            result == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(result);
    }

    // Retrieves a member's preferences
    public async Task<UserPreferences?>
        GetPreferencesAsync(string username)
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        // Find the user ID first
        int? userId =
            await GetUserIdAsync(
                connection,
                username);

        if (userId == null)
        {
            return null;
        }

        const string selectSql = @"
            SELECT Language, Theme, FontSize
            FROM dbo.UserPreferences
            WHERE UserID = @UserID;";

        await using var selectCommand =
            new SqlCommand(
                selectSql,
                connection);

        selectCommand.Parameters.AddWithValue(
            "@UserID",
            userId.Value);

        await using var reader =
            await selectCommand.ExecuteReaderAsync();

        // If preferences already exist, return them
        if (await reader.ReadAsync())
        {
            return new UserPreferences
            {
                UserId = userId.Value,
                Username = username,

                Language =
                    reader["Language"]?.ToString() ?? "en",

                Theme =
                    reader["Theme"]?.ToString() ?? "light",

                FontSize =
                    reader["FontSize"]?.ToString() ?? "medium"
            };
        }

        await reader.CloseAsync();

        // Create default preferences if none exist
        const string insertSql = @"
            INSERT INTO dbo.UserPreferences
                (UserID, Language, Theme, FontSize)
            VALUES
                (@UserID, 'en', 'light', 'medium');";

        await using var insertCommand =
            new SqlCommand(
                insertSql,
                connection);

        insertCommand.Parameters.AddWithValue(
            "@UserID",
            userId.Value);

        await insertCommand.ExecuteNonQueryAsync();

        // Return the default preferences
        return new UserPreferences
        {
            UserId = userId.Value,
            Username = username,
            Language = "en",
            Theme = "light",
            FontSize = "medium"
        };
    }

    // Saves or updates a member's preferences
    public async Task<bool> UpdatePreferencesAsync(
        string username,
        UpdateUserPreferencesRequest preferences)
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        // Find the user ID
        int? userId =
            await GetUserIdAsync(
                connection,
                username);

        if (userId == null)
        {
            return false;
        }

        // Try to update existing preferences
        const string updateSql = @"
            UPDATE dbo.UserPreferences
            SET
                Language = @Language,
                Theme = @Theme,
                FontSize = @FontSize
            WHERE UserID = @UserID;";

        await using var updateCommand =
            new SqlCommand(
                updateSql,
                connection);

        updateCommand.Parameters.AddWithValue(
            "@Language",
            preferences.Language);

        updateCommand.Parameters.AddWithValue(
            "@Theme",
            preferences.Theme);

        updateCommand.Parameters.AddWithValue(
            "@FontSize",
            preferences.FontSize);

        updateCommand.Parameters.AddWithValue(
            "@UserID",
            userId.Value);

        int rowsUpdated =
            await updateCommand.ExecuteNonQueryAsync();

        // If no row existed, create the preferences
        if (rowsUpdated == 0)
        {
            const string insertSql = @"
                INSERT INTO dbo.UserPreferences
                    (UserID, Language, Theme, FontSize)
                VALUES
                    (@UserID, @Language, @Theme, @FontSize);";

            await using var insertCommand =
                new SqlCommand(
                    insertSql,
                    connection);

            insertCommand.Parameters.AddWithValue(
                "@UserID",
                userId.Value);

            insertCommand.Parameters.AddWithValue(
                "@Language",
                preferences.Language);

            insertCommand.Parameters.AddWithValue(
                "@Theme",
                preferences.Theme);

            insertCommand.Parameters.AddWithValue(
                "@FontSize",
                preferences.FontSize);

            await insertCommand.ExecuteNonQueryAsync();
        }

        return true;
    }
}