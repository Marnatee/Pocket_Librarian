// Data access layer for the fulfillment queue.
// Manages fulfillment tasks for reserved books awaiting staff pickup for member collection.

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.DAL;

namespace Pocket_Librarian.DAL.Repositories;

public class FulfillmentQueueRepository : IFulfillmentQueueRepository
{
    private readonly string _configuredConnectionString;
    private string? _manualConnectionString;

    public FulfillmentQueueRepository(IConfiguration configuration)
    {
        _configuredConnectionString = configuration["ConnectionStrings:PocketLibrarianDatabase"] ?? string.Empty;
    }

    public void SetManualConnectionString(string connectionString)
    {
        _manualConnectionString = connectionString;
        DatabaseConnectionState.SetManualConnectionString(connectionString);
    }

    private string GetActiveConnectionString() => DatabaseConnectionState.GetActiveConnectionString(
        _manualConnectionString ?? _configuredConnectionString);

    public Task<List<FulfillmentTask>> GetAllPendingTasksAsync()
    {
        const string query = @"
            SELECT FulfillmentTaskID, ReservationID, UserID, BookID, AssignedStaffID,
                   BookTitle, MemberName, Status, DateCreated, DateAssigned, DateFulfilled
            FROM dbo.FulfillmentTasks
            WHERE Status IN ('Pending', 'InProgress')
            ORDER BY DateCreated ASC";

        return QueryFulfillmentTasksAsync(query, null);
    }

    public Task<List<FulfillmentTask>> GetPendingTasksForStaffAsync(int staffId)
    {
        const string query = @"
            SELECT FulfillmentTaskID, ReservationID, UserID, BookID, AssignedStaffID,
                   BookTitle, MemberName, Status, DateCreated, DateAssigned, DateFulfilled
            FROM dbo.FulfillmentTasks
            WHERE Status IN ('Pending', 'InProgress')
              AND AssignedStaffID = @StaffId
            ORDER BY DateCreated ASC";

        return QueryFulfillmentTasksAsync(query, command =>
            command.Parameters.AddWithValue("@StaffId", staffId));
    }

    public async Task<FulfillmentTask?> GetFulfillmentTaskByIdAsync(int fulfillmentTaskId)
    {
        const string query = @"
            SELECT FulfillmentTaskID, ReservationID, UserID, BookID, AssignedStaffID,
                   BookTitle, MemberName, Status, DateCreated, DateAssigned, DateFulfilled
            FROM dbo.FulfillmentTasks
            WHERE FulfillmentTaskID = @FulfillmentTaskId";

        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        command.Parameters.AddWithValue("@FulfillmentTaskId", fulfillmentTaskId);

        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadFulfillmentTask(reader) : null;
    }

    public async Task<int> CreateFulfillmentTaskAsync(FulfillmentTask task)
    {
        const string query = @"
            INSERT INTO dbo.FulfillmentTasks
                (ReservationID, UserID, BookID, AssignedStaffID, BookTitle, MemberName,
                 Status, DateCreated, DateAssigned, DateFulfilled)
            VALUES
                (@ReservationId, @UserId, @BookId, @AssignedStaffId, @BookTitle, @MemberName,
                 @Status, @DateCreated, @DateAssigned, @DateFulfilled);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        AddFulfillmentTaskParameters(command, task);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public Task<FulfillmentTask?> ClaimNextTaskAsync(int staffId)
    {
        const string query = @"
            ;WITH next_task AS
            (
                SELECT TOP (1) *
                FROM dbo.FulfillmentTasks WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = 'Pending' AND AssignedStaffID IS NULL
                ORDER BY DateCreated ASC
            )
            UPDATE next_task
            SET AssignedStaffID = @StaffId,
                Status = 'InProgress',
                DateAssigned = GETUTCDATE()
            OUTPUT INSERTED.FulfillmentTaskID, INSERTED.ReservationID, INSERTED.UserID, INSERTED.BookID,
                   INSERTED.AssignedStaffID, INSERTED.BookTitle, INSERTED.MemberName,
                   INSERTED.Status, INSERTED.DateCreated, INSERTED.DateAssigned, INSERTED.DateFulfilled";

        return ExecuteClaimAsync(query, command =>
            command.Parameters.AddWithValue("@StaffId", staffId));
    }

    public Task<FulfillmentTask?> ClaimTaskAsync(int fulfillmentTaskId, int staffId)
    {
        const string query = @"
            UPDATE task
            SET AssignedStaffID = @StaffId,
                Status = 'InProgress',
                DateAssigned = GETUTCDATE()
            OUTPUT INSERTED.FulfillmentTaskID, INSERTED.ReservationID, INSERTED.UserID, INSERTED.BookID,
                   INSERTED.AssignedStaffID, INSERTED.BookTitle, INSERTED.MemberName,
                   INSERTED.Status, INSERTED.DateCreated, INSERTED.DateAssigned, INSERTED.DateFulfilled
            FROM dbo.FulfillmentTasks AS task WITH (UPDLOCK, ROWLOCK)
            WHERE task.FulfillmentTaskID = @FulfillmentTaskId
              AND task.Status = 'Pending'
              AND task.AssignedStaffID IS NULL";

        return ExecuteClaimAsync(query, command =>
        {
            command.Parameters.AddWithValue("@FulfillmentTaskId", fulfillmentTaskId);
            command.Parameters.AddWithValue("@StaffId", staffId);
        });
    }

    public async Task<FulfillmentTask?> MarkTaskFulfilledAsync(int fulfillmentTaskId, int staffId)
    {
        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        using SqlTransaction transaction = connection.BeginTransaction();

        try
        {
            const string reservationQuery = @"
                SELECT task.ReservationID, task.UserID, task.BookTitle
                FROM dbo.FulfillmentTasks AS task WITH (UPDLOCK, ROWLOCK)
                WHERE task.FulfillmentTaskID = @FulfillmentTaskId
                  AND task.AssignedStaffID = @StaffId
                  AND task.Status IN ('Pending', 'InProgress')";

            int reservationId;
            int userId;
            string bookTitle;
            await using (SqlCommand reservationCommand = new(reservationQuery, connection, transaction))
            {
                reservationCommand.Parameters.AddWithValue("@FulfillmentTaskId", fulfillmentTaskId);
                reservationCommand.Parameters.AddWithValue("@StaffId", staffId);
                await using SqlDataReader reader = await reservationCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return null;
                }

                reservationId = reader.GetInt32(0);
                userId = reader.GetInt32(1);
                bookTitle = reader.GetString(2);
            }

            const string reservationStatusQuery = @"
                UPDATE dbo.Reservations
                SET ReservationStatus = 'ReadyForPickup'
                WHERE ReservationID = @ReservationId
                  AND UserID = @UserId
                  AND DateReturned IS NULL
                  AND ReservationStatus = 'Pending'";

            await using (SqlCommand statusCommand = new(reservationStatusQuery, connection, transaction))
            {
                statusCommand.Parameters.AddWithValue("@ReservationId", reservationId);
                statusCommand.Parameters.AddWithValue("@UserId", userId);
                if (await statusCommand.ExecuteNonQueryAsync() != 1)
                {
                    throw new InvalidOperationException(
                        "The fulfillment task was not linked to a pending reservation.");
                }
            }

            const string taskQuery = @"
                UPDATE dbo.FulfillmentTasks
                SET Status = 'Completed',
                    DateFulfilled = GETUTCDATE()
                WHERE FulfillmentTaskID = @FulfillmentTaskId
                  AND AssignedStaffID = @StaffId
                  AND Status IN ('Pending', 'InProgress')";

            await using (SqlCommand taskCommand = new(taskQuery, connection, transaction))
            {
                taskCommand.Parameters.AddWithValue("@FulfillmentTaskId", fulfillmentTaskId);
                taskCommand.Parameters.AddWithValue("@StaffId", staffId);
                if (await taskCommand.ExecuteNonQueryAsync() != 1)
                {
                    return null;
                }
            }

            const string notificationQuery = @"
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

                INSERT INTO dbo.Notifications (UserID, Title, MessageText)
                SELECT @UserId, N'Book ready for pickup', @MessageText
                FROM dbo.Users
                WHERE UserID = @UserId;

                SELECT @@ROWCOUNT;";

            await using (SqlCommand notificationCommand = new(notificationQuery, connection, transaction))
            {
                notificationCommand.Parameters.AddWithValue("@UserId", userId);
                notificationCommand.Parameters.AddWithValue(
                    "@MessageText",
                    $"{bookTitle} has been located and is ready for you to pick up.");

                int notificationCount = Convert.ToInt32(
                    await notificationCommand.ExecuteScalarAsync());
                if (notificationCount != 1)
                {
                    throw new InvalidOperationException(
                        "The member notification could not be created, so the fulfillment was not completed.");
                }
            }

            transaction.Commit();
            return await GetFulfillmentTaskByIdAsync(fulfillmentTaskId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<List<FulfillmentTask>> GetCompletedTasksAsync()
    {
        const string query = @"
            SELECT FulfillmentTaskID, ReservationID, UserID, BookID, AssignedStaffID,
                   BookTitle, MemberName, Status, DateCreated, DateAssigned, DateFulfilled
            FROM dbo.FulfillmentTasks
            WHERE Status = 'Completed'
            ORDER BY DateFulfilled DESC";

        return QueryFulfillmentTasksAsync(query, null);
    }

    // --- Helper Methods ---

    private async Task<List<FulfillmentTask>> QueryFulfillmentTasksAsync(
        string query,
        Action<SqlCommand>? parameterizer)
    {
        List<FulfillmentTask> tasks = [];

        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        parameterizer?.Invoke(command);

        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(ReadFulfillmentTask(reader));
        }

        return tasks;
    }

    private async Task<FulfillmentTask?> ExecuteClaimAsync(
        string query,
        Action<SqlCommand> parameterizer)
    {
        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        parameterizer(command);

        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadFulfillmentTask(reader) : null;
    }

    private static FulfillmentTask ReadFulfillmentTask(SqlDataReader reader)
    {
        return new FulfillmentTask
        {
            FulfillmentTaskId = reader.GetInt32(0),
            ReservationId = reader.GetInt32(1),
            UserId = reader.GetInt32(2),
            BookId = reader.GetInt32(3),
            AssignedStaffId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            BookTitle = reader.GetString(5),
            MemberName = reader.GetString(6),
            Status = reader.GetString(7),
            DateCreated = reader.GetDateTime(8),
            DateAssigned = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            DateFulfilled = reader.IsDBNull(10) ? null : reader.GetDateTime(10)
        };
    }

    private static void AddFulfillmentTaskParameters(SqlCommand command, FulfillmentTask task)
    {
        command.Parameters.AddWithValue("@ReservationId", task.ReservationId);
        command.Parameters.AddWithValue("@UserId", task.UserId);
        command.Parameters.AddWithValue("@BookId", task.BookId);
        command.Parameters.AddWithValue("@AssignedStaffId", task.AssignedStaffId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@BookTitle", task.BookTitle);
        command.Parameters.AddWithValue("@MemberName", task.MemberName);
        command.Parameters.AddWithValue("@Status", task.Status);
        command.Parameters.AddWithValue("@DateCreated", task.DateCreated);
        command.Parameters.AddWithValue("@DateAssigned", task.DateAssigned ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DateFulfilled", task.DateFulfilled ?? (object)DBNull.Value);
    }
}
