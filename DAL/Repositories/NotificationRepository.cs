using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.DAL.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly string _connectionString;

    public NotificationRepository(IConfiguration configuration)
    {
        _connectionString = configuration[
            "ConnectionStrings:PocketLibrarianDatabase"]
            ?? string.Empty;
    }

    public async Task<List<Notification>> GetForUserAsync(int userId)
    {
        await EnsureNotificationsTableAsync();

        const string sql = """
            SELECT NotificationID, Title, MessageText, CreatedDate, IsRead
            FROM dbo.Notifications
            WHERE UserID = @UserID
            ORDER BY CreatedDate DESC;
            """;

        await using SqlConnection connection =
            new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using SqlCommand command =
            new SqlCommand(sql, connection);
        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        List<Notification> notifications = new();

        while (await reader.ReadAsync())
        {
            notifications.Add(new Notification
            {
                NotificationId = reader.GetInt32(
                    reader.GetOrdinal("NotificationID")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                MessageText = reader.GetString(
                    reader.GetOrdinal("MessageText")),
                CreatedDate = reader.GetDateTime(
                    reader.GetOrdinal("CreatedDate")),
                IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead"))
            });
        }

        return notifications;
    }

    public async Task<bool> CreateForUserAsync(
        int userId,
        CreateNotificationRequest request)
    {
        await EnsureNotificationsTableAsync();

        const string sql = """
            INSERT INTO dbo.Notifications
                (UserID, Title, MessageText)
            SELECT @UserID, @Title, @MessageText
            FROM dbo.Users
            WHERE UserID = @UserID;
            """;

        await using SqlConnection connection =
            new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using SqlCommand command =
            new SqlCommand(sql, connection);
        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = request.Title;
        command.Parameters.Add("@MessageText", SqlDbType.NVarChar, 1000).Value = request.MessageText;

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> MarkAsReadAsync(
        int userId,
        int notificationId)
    {
        await EnsureNotificationsTableAsync();

        const string sql = """
            UPDATE dbo.Notifications
            SET IsRead = 1
            WHERE NotificationID = @NotificationID
              AND UserID = @UserID;
            """;

        await using SqlConnection connection =
            new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using SqlCommand command =
            new SqlCommand(sql, connection);
        command.Parameters.Add("@NotificationID", SqlDbType.Int).Value = notificationId;
        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> DeleteForUserAsync(
        int userId,
        int notificationId)
    {
        await EnsureNotificationsTableAsync();

        const string sql = """
            DELETE FROM dbo.Notifications
            WHERE NotificationID = @NotificationID
              AND UserID = @UserID;
            """;

        await using SqlConnection connection =
            new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using SqlCommand command =
            new SqlCommand(sql, connection);
        command.Parameters.Add("@NotificationID", SqlDbType.Int).Value = notificationId;
        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

        return await command.ExecuteNonQueryAsync() == 1;
    }

    private async Task EnsureNotificationsTableAsync()
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Notifications
                (
                    NotificationID INT IDENTITY(1,1) NOT NULL
                        CONSTRAINT PK_Notifications PRIMARY KEY,
                    UserID INT NOT NULL,
                    Title NVARCHAR(200) NOT NULL,
                    MessageText NVARCHAR(1000) NOT NULL,
                    CreatedDate DATETIME2 NOT NULL
                        CONSTRAINT DF_Notifications_CreatedDate
                        DEFAULT SYSUTCDATETIME(),
                    IsRead BIT NOT NULL
                        CONSTRAINT DF_Notifications_IsRead
                        DEFAULT 0,
                    CONSTRAINT FK_Notifications_Users
                        FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
                );

                CREATE INDEX IX_Notifications_UserID_CreatedDate
                    ON dbo.Notifications(UserID, CreatedDate DESC);
            END;
            """;

        await using SqlConnection connection =
            new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using SqlCommand command =
            new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
