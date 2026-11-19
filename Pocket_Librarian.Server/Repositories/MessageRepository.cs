using Microsoft.Data.SqlClient;
using Pocket_Librarian.Server.Models;

namespace Pocket_Librarian.Server.Repositories;

// Handles database operations involving messages
public class MessageRepository
{
    private readonly IConfiguration _configuration;

    // Used to create a notification when a message is sent
    private readonly NotificationRepository
        _notificationRepository;

    // Receives both repositories through dependency injection
    public MessageRepository(
        IConfiguration configuration,
        NotificationRepository notificationRepository)
    {
        _configuration = configuration;
        _notificationRepository =
            notificationRepository;
    }

    // Creates the Messages table if it does not exist
    public async Task EnsureMessagesTableAsync()
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        const string sql = @"
            IF OBJECT_ID('dbo.Messages', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Messages
                (
                    MessageID INT IDENTITY(1,1)
                        PRIMARY KEY,

                    SenderID INT NOT NULL,

                    RecipientID INT NOT NULL,

                    Subject NVARCHAR(200) NOT NULL,

                    MessageText NVARCHAR(1000) NOT NULL,

                    SentDate DATETIME2 NOT NULL
                        DEFAULT SYSDATETIME(),

                    IsRead BIT NOT NULL
                        DEFAULT 0,

                    FOREIGN KEY (SenderID)
                        REFERENCES dbo.[Library Table](id),

                    FOREIGN KEY (RecipientID)
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

    // Retrieves all messages for a member
    public async Task<List<Message>>
        GetInboxAsync(string username)
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        // Retrieves messages where the member is the recipient
        const string sql = @"
            SELECT
                m.MessageID,
                sender.username AS Sender,
                m.Subject,
                m.MessageText,
                m.SentDate,
                m.IsRead
            FROM dbo.Messages m
            INNER JOIN dbo.[Library Table] sender
                ON m.SenderID = sender.id
            INNER JOIN dbo.[Library Table] recipient
                ON m.RecipientID = recipient.id
            WHERE recipient.username = @Username
            ORDER BY m.SentDate DESC;";

        await using var command =
            new SqlCommand(sql, connection);

        // Safely supplies the username
        command.Parameters.AddWithValue(
            "@Username",
            username);

        await using var reader =
            await command.ExecuteReaderAsync();

        List<Message> messages = new();

        // Converts every database row into a Message object
        while (await reader.ReadAsync())
        {
            messages.Add(new Message
            {
                MessageId =
                    Convert.ToInt32(
                        reader["MessageID"]),

                Sender =
                    reader["Sender"]?.ToString() ?? "",

                Subject =
                    reader["Subject"]?.ToString() ?? "",

                MessageText =
                    reader["MessageText"]?.ToString() ?? "",

                SentDate =
                    Convert.ToDateTime(
                        reader["SentDate"]),

                IsRead =
                    Convert.ToBoolean(
                        reader["IsRead"])
            });
        }

        return messages;
    }

    // Sends a message and creates a notification
    // for the recipient
    public async Task<bool> SendMessageAsync(
        string senderUsername,
        string recipientUsername,
        CreateMessageRequest request)
    {
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        // Finds both users and inserts the message
        const string sql = @"
            INSERT INTO dbo.Messages
                (SenderID, RecipientID, Subject, MessageText)
            SELECT
                sender.id,
                recipient.id,
                @Subject,
                @MessageText
            FROM dbo.[Library Table] sender
            CROSS JOIN dbo.[Library Table] recipient
            WHERE sender.username = @SenderUsername
              AND recipient.username = @RecipientUsername;";

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@SenderUsername",
            senderUsername);

        command.Parameters.AddWithValue(
            "@RecipientUsername",
            recipientUsername);

        command.Parameters.AddWithValue(
            "@Subject",
            request.Subject);

        command.Parameters.AddWithValue(
            "@MessageText",
            request.MessageText);

        int rowsInserted =
            await command.ExecuteNonQueryAsync();

        // Stop if either username was not found
        if (rowsInserted == 0)
        {
            return false;
        }

        // Create a notification after the message is saved
        await _notificationRepository
            .CreateNotificationAsync(
                recipientUsername,
                new CreateNotificationRequest
                {
                    Title = "New Message",

                    MessageText =
                        $"You received a new message " +
                        $"from {senderUsername}."
                });

        return true;
    }
}