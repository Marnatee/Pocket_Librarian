// Data access layer for the returns queue.
// Multi-step return actions are kept in one SQL transaction so a task,
// inventory update, action log, and support ticket cannot partially succeed.

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.DAL;

namespace Pocket_Librarian.DAL.Repositories;

public class ReturnsQueueRepository : IReturnsQueueRepository
{
    private readonly string _configuredConnectionString;
    private string? _manualConnectionString;

    public ReturnsQueueRepository(IConfiguration configuration)
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

    public Task<List<ReturnTask>> GetPendingTasksForStaffAsync(int staffId)
    {
        const string query = @"
            SELECT ReturnTaskID, ReservationID, BookID, AssignedStaffID,
                   ShelfLocation, BookTitle, Status, DateCreated, DateClaimed, DateCompleted
            FROM dbo.ReturnTasks
            WHERE Status IN ('Pending', 'InProgress')
              AND AssignedStaffID = @StaffId
            ORDER BY DateCreated ASC";

        return QueryReturnTasksAsync(query, command => command.Parameters.AddWithValue("@StaffId", staffId));
    }

    public Task<List<ReturnTask>> GetAllPendingTasksAsync()
    {
        const string query = @"
            SELECT ReturnTaskID, ReservationID, BookID, AssignedStaffID,
                   ShelfLocation, BookTitle, Status, DateCreated, DateClaimed, DateCompleted
            FROM dbo.ReturnTasks
            WHERE Status IN ('Pending', 'InProgress')
            ORDER BY DateCreated ASC";

        return QueryReturnTasksAsync(query, null);
    }

    public async Task<ReturnTask?> GetReturnTaskByIdAsync(int returnTaskId)
    {
        const string query = @"
            SELECT ReturnTaskID, ReservationID, BookID, AssignedStaffID,
                   ShelfLocation, BookTitle, Status, DateCreated, DateClaimed, DateCompleted
            FROM dbo.ReturnTasks
            WHERE ReturnTaskID = @ReturnTaskId";

        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);

        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadReturnTask(reader) : null;
    }

    public async Task<int> CreateReturnTaskAsync(ReturnTask task)
    {
        const string query = @"
            INSERT INTO dbo.ReturnTasks
                (ReservationID, BookID, AssignedStaffID, ShelfLocation, BookTitle,
                 Status, DateCreated, DateClaimed, DateCompleted)
            VALUES
                (@ReservationId, @BookId, @AssignedStaffId, @ShelfLocation, @BookTitle,
                 @Status, @DateCreated, @DateClaimed, @DateCompleted);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        AddReturnTaskParameters(command, task);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public Task<ReturnTask?> ClaimNextTaskAsync(int staffId)
    {
        const string query = @"
            ;WITH next_task AS
            (
                SELECT TOP (1) *
                FROM dbo.ReturnTasks WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = 'Pending' AND AssignedStaffID IS NULL
                ORDER BY DateCreated ASC
            )
            UPDATE next_task
            SET AssignedStaffID = @StaffId,
                Status = 'InProgress',
                DateClaimed = GETUTCDATE()
            OUTPUT INSERTED.ReturnTaskID, INSERTED.ReservationID, INSERTED.BookID,
                   INSERTED.AssignedStaffID, INSERTED.ShelfLocation, INSERTED.BookTitle,
                   INSERTED.Status, INSERTED.DateCreated, INSERTED.DateClaimed, INSERTED.DateCompleted
            ;";

        return ExecuteClaimAsync(query, command => command.Parameters.AddWithValue("@StaffId", staffId));
    }

    public Task<ReturnTask?> ClaimTaskAsync(int returnTaskId, int staffId)
    {
        const string query = @"
            UPDATE task
            SET AssignedStaffID = @StaffId,
                Status = 'InProgress',
                DateClaimed = GETUTCDATE()
            OUTPUT INSERTED.ReturnTaskID, INSERTED.ReservationID, INSERTED.BookID,
                   INSERTED.AssignedStaffID, INSERTED.ShelfLocation, INSERTED.BookTitle,
                   INSERTED.Status, INSERTED.DateCreated, INSERTED.DateClaimed, INSERTED.DateCompleted
            FROM dbo.ReturnTasks AS task WITH (UPDLOCK, ROWLOCK)
            WHERE task.ReturnTaskID = @ReturnTaskId
              AND task.Status = 'Pending'
              AND task.AssignedStaffID IS NULL";

        return ExecuteClaimAsync(query, command =>
        {
            command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);
            command.Parameters.AddWithValue("@StaffId", staffId);
        });
    }

    public async Task<ReturnActionResponse> ProcessReturnActionAsync(
        int returnTaskId,
        int staffId,
        ReturnAction action,
        SupportTicket? supportTicket = null)
    {
        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        using SqlTransaction transaction = connection.BeginTransaction();

        try
        {
            ReturnTask? task = await GetTaskForUpdateAsync(connection, transaction, returnTaskId);
            if (task is null)
            {
                return Failure("Return task not found.");
            }

            if (task.Status == "Completed")
            {
                return Failure("This return task has already been completed.");
            }

            if (task.AssignedStaffId is not null && task.AssignedStaffId != staffId)
            {
                return Failure("This task is assigned to another staff member.");
            }

            if (task.AssignedStaffId is null)
            {
                task.AssignedStaffId = staffId;
                await ClaimTaskWithinTransactionAsync(connection, transaction, returnTaskId, staffId);
            }

            int? newTaskId = null;
            int? supportTicketId = null;

            switch (action.ActionType)
            {
                case "Complete":
                    await RestockBookAsync(connection, transaction, task.BookId);
                    break;

                case "AnotherBook":
                    if (action.FoundBookId is null || string.IsNullOrWhiteSpace(action.FoundBookTitle)
                        || string.IsNullOrWhiteSpace(action.FoundBookShelfLocation))
                    {
                        return Failure("A valid found-book ID, title, and shelf location are required.");
                    }

                    newTaskId = await InsertFoundBookTaskAsync(
                        connection,
                        transaction,
                        action.FoundBookId.Value,
                        action.FoundBookTitle,
                        action.FoundBookShelfLocation,
                        staffId);
                    break;

                case "BadCondition":
                    await RemoveDamagedCopyFromInventoryAsync(connection, transaction, task.BookId);
                    break;

                case "Other":
                    if (supportTicket is null || string.IsNullOrWhiteSpace(supportTicket.Description))
                    {
                        return Failure("A support-ticket description is required.");
                    }

                    supportTicket.ReturnTaskId = returnTaskId;
                    supportTicket.BookId = task.BookId;
                    supportTicketId = await InsertSupportTicketAsync(connection, transaction, supportTicket);
                    break;

                default:
                    return Failure("Unsupported return action.");
            }

            if (task.ReservationId.HasValue)
            {
                await FinalizeReservationReturnWithinTransactionAsync(
                    connection,
                    transaction,
                    task.ReservationId.Value);
            }

            await InsertReturnActionAsync(connection, transaction, returnTaskId, action);
            await CompleteTaskWithinTransactionAsync(connection, transaction, returnTaskId, staffId);
            transaction.Commit();

            var response = new ReturnActionResponse
            {
                Success = true,
                Message = GetSuccessMessage(action.ActionType),
                CompletedTask = await GetReturnTaskByIdAsync(returnTaskId)
            };

            if (newTaskId is not null)
            {
                response.NewTask = await GetReturnTaskByIdAsync(newTaskId.Value);
            }

            if (supportTicketId is not null)
            {
                response.SupportTicket = await GetSupportTicketByIdAsync(supportTicketId.Value);
            }

            return response;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<SupportTicket?> GetSupportTicketByIdAsync(int ticketId)
    {
        const string query = @"
            SELECT SupportTicketID, ReturnTaskID, BookID, CreatedByStaffID, AssignedStaffID,
                   Description, Status, DateCreated, DateResolved, ResolutionNotes
            FROM dbo.SupportTickets
            WHERE SupportTicketID = @TicketId";

        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        command.Parameters.AddWithValue("@TicketId", ticketId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadSupportTicket(reader) : null;
    }

    public async Task<List<SupportTicket>> GetSupportTicketsAsync(string? status = null)
    {
        const string query = @"
            SELECT SupportTicketID, ReturnTaskID, BookID, CreatedByStaffID, AssignedStaffID,
                   Description, Status, DateCreated, DateResolved, ResolutionNotes
            FROM dbo.SupportTickets
            WHERE (@Status IS NULL OR Status = @Status)
            ORDER BY DateCreated DESC";

        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        command.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();

        var tickets = new List<SupportTicket>();
        while (await reader.ReadAsync())
        {
            tickets.Add(ReadSupportTicket(reader));
        }

        return tickets;
    }

    public async Task<bool> ResolveSupportTicketAsync(int ticketId, int staffId, string resolutionNotes)
    {
        const string query = @"
            UPDATE dbo.SupportTickets
            SET Status = 'Resolved',
                AssignedStaffID = @StaffId,
                DateResolved = GETUTCDATE(),
                ResolutionNotes = @ResolutionNotes
            WHERE SupportTicketID = @TicketId
              AND Status IN ('Open', 'InProgress')";

        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        command.Parameters.AddWithValue("@TicketId", ticketId);
        command.Parameters.AddWithValue("@StaffId", staffId);
        command.Parameters.AddWithValue("@ResolutionNotes", resolutionNotes);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private async Task<List<ReturnTask>> QueryReturnTasksAsync(
        string query,
        Action<SqlCommand>? addParameters)
    {
        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        addParameters?.Invoke(command);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();

        var tasks = new List<ReturnTask>();
        while (await reader.ReadAsync())
        {
            tasks.Add(ReadReturnTask(reader));
        }

        return tasks;
    }

    private async Task<ReturnTask?> ExecuteClaimAsync(string query, Action<SqlCommand> addParameters)
    {
        await using SqlConnection connection = new(GetActiveConnectionString());
        await connection.OpenAsync();
        await using SqlCommand command = new(query, connection);
        addParameters(command);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadReturnTask(reader) : null;
    }

    private static async Task<ReturnTask?> GetTaskForUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int returnTaskId)
    {
        const string query = @"
            SELECT ReturnTaskID, ReservationID, BookID, AssignedStaffID,
                   ShelfLocation, BookTitle, Status, DateCreated, DateClaimed, DateCompleted
            FROM dbo.ReturnTasks WITH (UPDLOCK, ROWLOCK)
            WHERE ReturnTaskID = @ReturnTaskId";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadReturnTask(reader) : null;
    }

    private static async Task ClaimTaskWithinTransactionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int returnTaskId,
        int staffId)
    {
        const string query = @"
            UPDATE dbo.ReturnTasks
            SET AssignedStaffID = @StaffId,
                Status = 'InProgress',
                DateClaimed = GETUTCDATE()
            WHERE ReturnTaskID = @ReturnTaskId AND Status <> 'Completed'";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);
        command.Parameters.AddWithValue("@StaffId", staffId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CompleteTaskWithinTransactionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int returnTaskId,
        int staffId)
    {
        const string query = @"
            UPDATE dbo.ReturnTasks
            SET Status = 'Completed',
                AssignedStaffID = @StaffId,
                DateCompleted = GETUTCDATE()
            WHERE ReturnTaskID = @ReturnTaskId AND Status <> 'Completed'";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);
        command.Parameters.AddWithValue("@StaffId", staffId);
        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException("The return task could not be completed.");
        }
    }

    private static async Task FinalizeReservationReturnWithinTransactionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int reservationId)
    {
        const string query = @"
            UPDATE dbo.Reservations
            SET DateReturned = GETUTCDATE(),
                ReservationStatus = 'Returned'
            WHERE ReservationID = @ReservationId
              AND DateReturned IS NULL
              AND ReservationStatus = 'ReturnPending'";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@ReservationId", reservationId);
        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException(
                "The return task was not linked to a return-pending reservation.");
        }
    }

    private static async Task RestockBookAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int bookId)
    {
        const string query = @"
            UPDATE dbo.Books
            SET CopiesAvailable = CASE
                WHEN CopiesAvailable < TotalCopies THEN CopiesAvailable + 1
                ELSE TotalCopies
            END
            WHERE BookID = @BookId";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@BookId", bookId);
        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException("The returned book could not be restocked.");
        }
    }

    private static async Task RemoveDamagedCopyFromInventoryAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int bookId)
    {
        const string query = @"
            UPDATE dbo.Books
            SET TotalCopies = CASE WHEN TotalCopies > 0 THEN TotalCopies - 1 ELSE 0 END,
                CopiesAvailable = CASE
                    WHEN CopiesAvailable > CASE WHEN TotalCopies > 0 THEN TotalCopies - 1 ELSE 0 END
                    THEN CASE WHEN TotalCopies > 0 THEN TotalCopies - 1 ELSE 0 END
                    ELSE CopiesAvailable
                END
            WHERE BookID = @BookId AND TotalCopies > 0";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@BookId", bookId);
        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException("The damaged book could not be removed from inventory.");
        }
    }

    private static async Task<int> InsertFoundBookTaskAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int bookId,
        string title,
        string shelfLocation,
        int staffId)
    {
        const string query = @"
            INSERT INTO dbo.ReturnTasks
                (ReservationID, BookID, AssignedStaffID, ShelfLocation, BookTitle,
                 Status, DateCreated, DateClaimed, DateCompleted)
            VALUES
                (NULL, @BookId, @StaffId, @ShelfLocation, @BookTitle,
                 'InProgress', GETUTCDATE(), GETUTCDATE(), NULL);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@BookId", bookId);
        command.Parameters.AddWithValue("@StaffId", staffId);
        command.Parameters.AddWithValue("@ShelfLocation", shelfLocation);
        command.Parameters.AddWithValue("@BookTitle", title);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<int> InsertReturnActionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int returnTaskId,
        ReturnAction action)
    {
        const string query = @"
            INSERT INTO dbo.ReturnActions
                (ReturnTaskID, ActionType, FoundBookID, FoundBookTitle,
                 FoundBookShelfLocation, SupportTicketDescription, DateActioned, StaffID)
            VALUES
                (@ReturnTaskId, @ActionType, @FoundBookId, @FoundBookTitle,
                 @FoundBookShelfLocation, @SupportTicketDescription, @DateActioned, @StaffId);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);
        command.Parameters.AddWithValue("@ActionType", action.ActionType);
        command.Parameters.AddWithValue("@FoundBookId", action.FoundBookId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@FoundBookTitle", action.FoundBookTitle ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@FoundBookShelfLocation", action.FoundBookShelfLocation ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupportTicketDescription", action.SupportTicketDescription ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DateActioned", action.DateActioned);
        command.Parameters.AddWithValue("@StaffId", action.StaffId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<int> InsertSupportTicketAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        SupportTicket ticket)
    {
        const string query = @"
            INSERT INTO dbo.SupportTickets
                (ReturnTaskID, BookID, CreatedByStaffID, AssignedStaffID, Description,
                 Status, DateCreated, DateResolved, ResolutionNotes)
            VALUES
                (@ReturnTaskId, @BookId, @CreatedByStaffId, @AssignedStaffId, @Description,
                 @Status, @DateCreated, @DateResolved, @ResolutionNotes);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        await using SqlCommand command = new(query, connection, transaction);
        command.Parameters.AddWithValue("@ReturnTaskId", ticket.ReturnTaskId);
        command.Parameters.AddWithValue("@BookId", ticket.BookId);
        command.Parameters.AddWithValue("@CreatedByStaffId", ticket.CreatedByStaffId);
        command.Parameters.AddWithValue("@AssignedStaffId", ticket.AssignedStaffId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Description", ticket.Description);
        command.Parameters.AddWithValue("@Status", ticket.Status);
        command.Parameters.AddWithValue("@DateCreated", ticket.DateCreated);
        command.Parameters.AddWithValue("@DateResolved", ticket.DateResolved ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ResolutionNotes", ticket.ResolutionNotes ?? (object)DBNull.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static void AddReturnTaskParameters(SqlCommand command, ReturnTask task)
    {
        command.Parameters.AddWithValue("@ReservationId", task.ReservationId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@BookId", task.BookId);
        command.Parameters.AddWithValue("@AssignedStaffId", task.AssignedStaffId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ShelfLocation", task.ShelfLocation);
        command.Parameters.AddWithValue("@BookTitle", task.BookTitle);
        command.Parameters.AddWithValue("@Status", task.Status);
        command.Parameters.AddWithValue("@DateCreated", task.DateCreated);
        command.Parameters.AddWithValue("@DateClaimed", task.DateClaimed ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DateCompleted", task.DateCompleted ?? (object)DBNull.Value);
    }

    private static ReturnTask ReadReturnTask(SqlDataReader reader) => new()
    {
        ReturnTaskId = reader.GetInt32(0),
        ReservationId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
        BookId = reader.GetInt32(2),
        AssignedStaffId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
        ShelfLocation = reader.GetString(4),
        BookTitle = reader.GetString(5),
        Status = reader.GetString(6),
        DateCreated = reader.GetDateTime(7),
        DateClaimed = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
        DateCompleted = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
    };

    private static SupportTicket ReadSupportTicket(SqlDataReader reader) => new()
    {
        SupportTicketId = reader.GetInt32(0),
        ReturnTaskId = reader.GetInt32(1),
        BookId = reader.GetInt32(2),
        CreatedByStaffId = reader.GetInt32(3),
        AssignedStaffId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
        Description = reader.GetString(5),
        Status = reader.GetString(6),
        DateCreated = reader.GetDateTime(7),
        DateResolved = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
        ResolutionNotes = reader.IsDBNull(9) ? null : reader.GetString(9)
    };

    private static ReturnActionResponse Failure(string message) => new()
    {
        Success = false,
        Message = message
    };

    private static string GetSuccessMessage(string actionType) => actionType switch
    {
        "Complete" => "Return completed and book restocked.",
        "AnotherBook" => "Return completed and a new found-book task was created.",
        "BadCondition" => "Return completed and the damaged copy was removed from inventory.",
        "Other" => "Return completed and a support ticket was created.",
        _ => "Return action completed."
    };
}
