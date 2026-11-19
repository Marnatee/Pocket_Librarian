// Data access layer (SQL Database)
// This layer communicates with the SQL Database to perform CRUD operations on returns queue.
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.DAL.Repositories
{
    public class ReturnsQueueRepository : IReturnsQueueRepository
    {
        private readonly string _configuredConnectionString;
        private string? _manualConnectionString;

        public ReturnsQueueRepository(IConfiguration configuration)
        {
            _configuredConnectionString = configuration["ConnectionStrings:PocketLibrarianDatabase"]
                ?? string.Empty;
        }

        public void SetManualConnectionString(string connectionString)
        {
            _manualConnectionString = connectionString;
        }

        private string GetActiveConnectionString()
        {
            return _manualConnectionString ?? _configuredConnectionString;
        }

        public async Task<List<ReturnTask>> GetPendingTasksForStaffAsync(int staffId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT ReturnTaskID, ReservationID, BookID, AssignedStaffID, 
                               ShelfLocation, BookTitle, Status, DateCreated, DateCompleted
                        FROM dbo.ReturnTasks
                        WHERE Status = 'Pending' 
                          AND AssignedStaffID = @StaffId
                        ORDER BY DateCreated ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StaffId", staffId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            return await ReadReturnTasksAsync(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving pending tasks for staff {staffId}: {ex.Message}", ex);
            }
        }

        public async Task<List<ReturnTask>> GetAllPendingTasksAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT ReturnTaskID, ReservationID, BookID, AssignedStaffID, 
                               ShelfLocation, BookTitle, Status, DateCreated, DateCompleted
                        FROM dbo.ReturnTasks
                        WHERE Status = 'Pending'
                        ORDER BY DateCreated ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            return await ReadReturnTasksAsync(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving all pending tasks: {ex.Message}", ex);
            }
        }

        public async Task<ReturnTask?> GetReturnTaskByIdAsync(int returnTaskId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT ReturnTaskID, ReservationID, BookID, AssignedStaffID, 
                               ShelfLocation, BookTitle, Status, DateCreated, DateCompleted
                        FROM dbo.ReturnTasks
                        WHERE ReturnTaskID = @ReturnTaskId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return ReadReturnTask(reader);
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving return task {returnTaskId}: {ex.Message}", ex);
            }
        }

        public async Task<int> CreateReturnTaskAsync(ReturnTask task)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        INSERT INTO dbo.ReturnTasks 
                            (ReservationID, BookID, AssignedStaffID, ShelfLocation, BookTitle, Status, DateCreated, DateCompleted)
                        VALUES 
                            (@ReservationId, @BookId, @AssignedStaffId, @ShelfLocation, @BookTitle, @Status, @DateCreated, @DateCompleted)

                        SELECT CAST(SCOPE_IDENTITY() as int)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReservationId", task.ReservationId);
                        command.Parameters.AddWithValue("@BookId", task.BookId);
                        command.Parameters.AddWithValue("@AssignedStaffId", task.AssignedStaffId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ShelfLocation", task.ShelfLocation);
                        command.Parameters.AddWithValue("@BookTitle", task.BookTitle);
                        command.Parameters.AddWithValue("@Status", task.Status);
                        command.Parameters.AddWithValue("@DateCreated", task.DateCreated);
                        command.Parameters.AddWithValue("@DateCompleted", task.DateCompleted ?? (object)DBNull.Value);

                        object? result = await command.ExecuteScalarAsync();
                        return result is not null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error creating return task: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateReturnTaskAsync(ReturnTask task)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        UPDATE dbo.ReturnTasks
                        SET ReservationID = @ReservationId,
                            BookID = @BookId,
                            AssignedStaffID = @AssignedStaffId,
                            ShelfLocation = @ShelfLocation,
                            BookTitle = @BookTitle,
                            Status = @Status,
                            DateCompleted = @DateCompleted
                        WHERE ReturnTaskID = @ReturnTaskId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReturnTaskId", task.ReturnTaskId);
                        command.Parameters.AddWithValue("@ReservationId", task.ReservationId);
                        command.Parameters.AddWithValue("@BookId", task.BookId);
                        command.Parameters.AddWithValue("@AssignedStaffId", task.AssignedStaffId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ShelfLocation", task.ShelfLocation);
                        command.Parameters.AddWithValue("@BookTitle", task.BookTitle);
                        command.Parameters.AddWithValue("@Status", task.Status);
                        command.Parameters.AddWithValue("@DateCompleted", task.DateCompleted ?? (object)DBNull.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating return task: {ex.Message}", ex);
            }
        }

        public async Task<bool> CompleteReturnTaskAsync(int returnTaskId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        UPDATE dbo.ReturnTasks
                        SET Status = 'Completed',
                            DateCompleted = GETUTCDATE()
                        WHERE ReturnTaskID = @ReturnTaskId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error completing return task: {ex.Message}", ex);
            }
        }

        public async Task<int> RecordReturnActionAsync(ReturnAction action, int returnTaskId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        INSERT INTO dbo.ReturnActions 
                            (ReturnTaskID, ActionType, FoundBookTitle, FoundBookShelfLocation, 
                             SupportTicketDescription, DateActioned, StaffID)
                        VALUES 
                            (@ReturnTaskId, @ActionType, @FoundBookTitle, @FoundBookShelfLocation,
                             @SupportTicketDescription, @DateActioned, @StaffId)

                        SELECT CAST(SCOPE_IDENTITY() as int)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReturnTaskId", returnTaskId);
                        command.Parameters.AddWithValue("@ActionType", action.ActionType);
                        command.Parameters.AddWithValue("@FoundBookTitle", action.FoundBookTitle ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@FoundBookShelfLocation", action.FoundBookShelfLocation ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SupportTicketDescription", action.SupportTicketDescription ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DateActioned", action.DateActioned);
                        command.Parameters.AddWithValue("@StaffId", action.StaffId);

                        object? result = await command.ExecuteScalarAsync();
                        return result is not null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error recording return action: {ex.Message}", ex);
            }
        }

        public async Task<int> CreateSupportTicketAsync(SupportTicket ticket)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        INSERT INTO dbo.SupportTickets 
                            (ReturnTaskID, BookID, CreatedByStaffID, Description, Status, 
                             DateCreated, DateResolved, ResolutionNotes)
                        VALUES 
                            (@ReturnTaskId, @BookId, @CreatedByStaffId, @Description, @Status,
                             @DateCreated, @DateResolved, @ResolutionNotes)

                        SELECT CAST(SCOPE_IDENTITY() as int)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReturnTaskId", ticket.ReturnTaskId);
                        command.Parameters.AddWithValue("@BookId", ticket.BookId);
                        command.Parameters.AddWithValue("@CreatedByStaffId", ticket.CreatedByStaffId);
                        command.Parameters.AddWithValue("@Description", ticket.Description);
                        command.Parameters.AddWithValue("@Status", ticket.Status);
                        command.Parameters.AddWithValue("@DateCreated", ticket.DateCreated);
                        command.Parameters.AddWithValue("@DateResolved", ticket.DateResolved ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ResolutionNotes", ticket.ResolutionNotes ?? (object)DBNull.Value);

                        object? result = await command.ExecuteScalarAsync();
                        return result is not null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error creating support ticket: {ex.Message}", ex);
            }
        }

        public async Task<SupportTicket?> GetSupportTicketByIdAsync(int ticketId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT SupportTicketID, ReturnTaskID, BookID, CreatedByStaffID, Description, 
                               Status, DateCreated, DateResolved, ResolutionNotes
                        FROM dbo.SupportTickets
                        WHERE SupportTicketID = @TicketId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TicketId", ticketId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return ReadSupportTicket(reader);
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving support ticket {ticketId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> FlagBookForDeletionAsync(int bookId, string reason)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    // Update Books table to flag for deletion
                    // This assumes a 'IsDeleted' or 'Status' column exists
                    // Adjust column names based on actual schema
                    string query = @"
                        UPDATE dbo.Books
                        SET IsDeleted = 1,
                            DeletionReason = @Reason,
                            DeletionDate = GETUTCDATE(),
                            CopiesAvailable = 0
                        WHERE BookID = @BookId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BookId", bookId);
                        command.Parameters.AddWithValue("@Reason", reason);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error flagging book for deletion: {ex.Message}", ex);
            }
        }

        public async Task<List<SupportTicket>> GetSupportTicketsAsync(string? status = null)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT SupportTicketID, ReturnTaskID, BookID, CreatedByStaffID, Description, 
                               Status, DateCreated, DateResolved, ResolutionNotes
                        FROM dbo.SupportTickets
                        WHERE (@Status IS NULL OR Status = @Status)
                        ORDER BY DateCreated DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            return await ReadSupportTicketsAsync(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving support tickets: {ex.Message}", ex);
            }
        }

        // Helper methods
        private ReturnTask ReadReturnTask(SqlDataReader reader)
        {
            return new ReturnTask
            {
                ReturnTaskId = reader.GetInt32(0),
                ReservationId = reader.GetInt32(1),
                BookId = reader.GetInt32(2),
                AssignedStaffId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                ShelfLocation = reader.GetString(4),
                BookTitle = reader.GetString(5),
                Status = reader.GetString(6),
                DateCreated = reader.GetDateTime(7),
                DateCompleted = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            };
        }

        private async Task<List<ReturnTask>> ReadReturnTasksAsync(SqlDataReader reader)
        {
            var tasks = new List<ReturnTask>();
            while (await reader.ReadAsync())
            {
                tasks.Add(ReadReturnTask(reader));
            }
            return tasks;
        }

        private SupportTicket ReadSupportTicket(SqlDataReader reader)
        {
            return new SupportTicket
            {
                SupportTicketId = reader.GetInt32(0),
                ReturnTaskId = reader.GetInt32(1),
                BookId = reader.GetInt32(2),
                CreatedByStaffId = reader.GetInt32(3),
                Description = reader.GetString(4),
                Status = reader.GetString(5),
                DateCreated = reader.GetDateTime(6),
                DateResolved = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                ResolutionNotes = reader.IsDBNull(8) ? null : reader.GetString(8)
            };
        }

        private async Task<List<SupportTicket>> ReadSupportTicketsAsync(SqlDataReader reader)
        {
            var tickets = new List<SupportTicket>();
            while (await reader.ReadAsync())
            {
                tickets.Add(ReadSupportTicket(reader));
            }
            return tickets;
        }
    }
}
