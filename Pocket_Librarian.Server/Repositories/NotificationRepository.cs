using Microsoft.Data.SqlClient;
using Pocket_Librarian.Server.Models;

namespace Pocket_Librarian.Server.Repositories;

// Handles notification-related database operations
public class NotificationRepository
{
    private readonly IConfiguration _configuration;

    public NotificationRepository(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Creates the Notifications table if it does not exist
    public async Task EnsureNotificationsTableAsync()
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        const string sql = @"
            IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Notifications
                (
                    NotificationID INT IDENTITY(1,1)
                        PRIMARY KEY,

                    UserID INT NOT NULL,

                    Title NVARCHAR(200) NOT NULL,

                    MessageText NVARCHAR(1000) NOT NULL,

                    CreatedDate DATETIME2 NOT NULL
                        DEFAULT SYSDATETIME(),

                    IsRead BIT NOT NULL
                        DEFAULT 0,

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

    // Retrieves all notifications for a member
    public async Task<List<Notification>>
        GetNotificationsAsync(string username)
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = @"
            SELECT
                n.NotificationID,
                n.Title,
                n.MessageText,
                n.CreatedDate,
                n.IsRead
            FROM dbo.Notifications n
            INNER JOIN dbo.[Library Table] u
                ON n.UserID = u.id
            WHERE u.username = @Username
            ORDER BY n.CreatedDate DESC;";

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Username",
            username);

        await using var reader =
            await command.ExecuteReaderAsync();

        List<Notification> notifications = new();

        while (await reader.ReadAsync())
        {
            notifications.Add(new Notification
            {
                NotificationId =
                    Convert.ToInt32(
                        reader["NotificationID"]),

                Title =
                    reader["Title"]?.ToString() ?? "",

                MessageText =
                    reader["MessageText"]?.ToString() ?? "",

                CreatedDate =
                    Convert.ToDateTime(
                        reader["CreatedDate"]),

                IsRead =
                    Convert.ToBoolean(
                        reader["IsRead"])
            });
        }

        return notifications;
    }

    // Creates a notification for a specific member
    public async Task<bool> CreateNotificationAsync(
        string username,
        CreateNotificationRequest request)
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO dbo.Notifications
                (UserID, Title, MessageText)
            SELECT
                id,
                @Title,
                @MessageText
            FROM dbo.[Library Table]
            WHERE username = @Username;";

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Username",
            username);

        command.Parameters.AddWithValue(
            "@Title",
            request.Title);

        command.Parameters.AddWithValue(
            "@MessageText",
            request.MessageText);

        int rowsInserted =
            await command.ExecuteNonQueryAsync();

        return rowsInserted > 0;
    }

    // Marks one notification as read
    public async Task<bool> MarkAsReadAsync(
        string username,
        int notificationId)
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = @"
            UPDATE n
            SET IsRead = 1
            FROM dbo.Notifications n
            INNER JOIN dbo.[Library Table] u
                ON n.UserID = u.id
            WHERE n.NotificationID = @NotificationID
              AND u.username = @Username;";

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@NotificationID",
            notificationId);

        command.Parameters.AddWithValue(
            "@Username",
            username);

        int rowsUpdated =
            await command.ExecuteNonQueryAsync();

        return rowsUpdated > 0;
    }
}