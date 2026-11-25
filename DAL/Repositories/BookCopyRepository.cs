using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.DAL.Repositories;

public sealed class BookCopyRepository : IBookCopyRepository
{
    private readonly string _connectionString;

    public BookCopyRepository(IConfiguration configuration)
    {
        _connectionString = configuration["ConnectionStrings:PocketLibrarianDatabase"] ?? string.Empty;
    }

    public Task<List<BookCopy>> GetCopiesByBookIdAsync(int bookId) =>
        ReadCopiesAsync("WHERE BookID = @BookID ORDER BY CopyID", ("@BookID", bookId));

    public async Task<BookCopy?> GetCopyByIdAsync(int copyId)
    {
        var copies = await ReadCopiesAsync("WHERE CopyID = @CopyID", ("@CopyID", copyId));
        return copies.SingleOrDefault();
    }

    public Task<List<BookCopy>> GetAvailableCopiesByBookIdAsync(int bookId) =>
        ReadCopiesAsync("WHERE BookID = @BookID AND Status = 'Available' ORDER BY CopyID", ("@BookID", bookId));

    public async Task<BookCopy?> GetCopyByBarcodeAsync(string barcode)
    {
        var copies = await ReadCopiesAsync("WHERE Barcode = @Barcode", ("@Barcode", barcode));
        return copies.SingleOrDefault();
    }

    public async Task<int> CreateCopyAsync(BookCopy copy)
    {
        var ids = await CreateCopiesAsync([copy]);
        return ids[0];
    }

    public async Task<List<int>> CreateCopiesAsync(int bookId, int count, string condition, string? location)
    {
        var copies = Enumerable.Range(0, count)
            .Select(_ => new BookCopy
            {
                BookId = bookId,
                Condition = condition,
                Status = "Available",
                Location = location
            })
            .ToList();
        return await CreateCopiesAsync(copies);
    }

    public async Task<List<int>> CreateCopiesAsync(List<BookCopy> copies)
    {
        if (copies.Count == 0) return [];
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            var ids = new List<int>(copies.Count);
            const string sql = """
                INSERT INTO dbo.BookCopies
                    (BookID, Barcode, Condition, Status, Location, CurrentReservationID,
                     CheckoutDueDate, DateAcquired, Notes, DateModified)
                OUTPUT INSERTED.CopyID
                VALUES
                    (@BookID, @Barcode, @Condition, @Status, @Location, @ReservationID,
                     @DueDate, COALESCE(@DateAcquired, SYSUTCDATETIME()), @Notes, SYSUTCDATETIME());
                """;
            foreach (var copy in copies)
            {
                await using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.Add("@BookID", SqlDbType.Int).Value = copy.BookId;
                AddNullable(command, "@Barcode", SqlDbType.NVarChar, 100, copy.Barcode);
                command.Parameters.Add("@Condition", SqlDbType.NVarChar, 30).Value = copy.Condition;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = copy.Status;
                AddNullable(command, "@Location", SqlDbType.NVarChar, 255, copy.Location);
                AddNullable(command, "@ReservationID", SqlDbType.Int, null, copy.CurrentReservationId);
                AddNullable(command, "@DueDate", SqlDbType.DateTime2, null, copy.CheckoutDueDate);
                AddNullable(command, "@DateAcquired", SqlDbType.DateTime2, null, copy.DateAcquired);
                AddNullable(command, "@Notes", SqlDbType.NVarChar, -1, copy.Notes);
                ids.Add(Convert.ToInt32(await command.ExecuteScalarAsync()));
            }
            foreach (var bookId in copies.Select(copy => copy.BookId).Distinct())
                await RefreshInventoryAsync(connection, transaction, bookId);
            await transaction.CommitAsync();
            return ids;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateCopyAsync(BookCopy copy)
    {
        const string sql = """
            UPDATE dbo.BookCopies
            SET Barcode = @Barcode,
                Condition = @Condition,
                Status = @Status,
                Location = @Location,
                CurrentReservationID = @ReservationID,
                CheckoutDueDate = @DueDate,
                Notes = @Notes,
                DateModified = SYSUTCDATETIME()
            WHERE CopyID = @CopyID;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@CopyID", SqlDbType.Int).Value = copy.CopyId;
            AddNullable(command, "@Barcode", SqlDbType.NVarChar, 100, copy.Barcode);
            command.Parameters.Add("@Condition", SqlDbType.NVarChar, 30).Value = copy.Condition;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = copy.Status;
            AddNullable(command, "@Location", SqlDbType.NVarChar, 255, copy.Location);
            AddNullable(command, "@ReservationID", SqlDbType.Int, null, copy.CurrentReservationId);
            AddNullable(command, "@DueDate", SqlDbType.DateTime2, null, copy.CheckoutDueDate);
            AddNullable(command, "@Notes", SqlDbType.NVarChar, -1, copy.Notes);
            var updated = await command.ExecuteNonQueryAsync() == 1;
            if (updated)
            {
                var bookId = await GetBookIdForCopyAsync(connection, transaction, copy.CopyId);
                if (bookId.HasValue) await RefreshInventoryAsync(connection, transaction, bookId.Value);
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
            return updated;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public Task<bool> UpdateCopyStatusAsync(int copyId, string newStatus) =>
        UpdateSingleFieldAsync(copyId, "Status", newStatus);

    public Task<bool> UpdateCopyConditionAsync(int copyId, string newCondition) =>
        UpdateSingleFieldAsync(copyId, "Condition", newCondition);

    public Task<bool> UpdateCopyLocationAsync(int copyId, string? location) =>
        UpdateSingleFieldAsync(copyId, "Location", location);

    public async Task<bool> CheckOutCopyAsync(int copyId, int reservationId, DateTime dueDate)
    {
        const string sql = """
            UPDATE dbo.BookCopies
            SET Status = 'Checked Out',
                CurrentReservationID = @ReservationID,
                CheckoutDueDate = @DueDate,
                DateModified = SYSUTCDATETIME()
            WHERE CopyID = @CopyID AND Status = 'Available';
            """;
        return await UpdateWithInventoryAsync(sql, copyId, ("@ReservationID", reservationId), ("@DueDate", dueDate));
    }

    public async Task<bool> ReturnCopyAsync(int copyId)
    {
        const string sql = """
            UPDATE dbo.BookCopies
            SET Status = 'Available',
                CurrentReservationID = NULL,
                CheckoutDueDate = NULL,
                DateModified = SYSUTCDATETIME()
            WHERE CopyID = @CopyID AND Status = 'Checked Out';
            """;
        return await UpdateWithInventoryAsync(sql, copyId);
    }

    public async Task<bool> DeleteCopyAsync(int copyId)
    {
        const string sql = """
            DELETE FROM dbo.BookCopies
            WHERE CopyID = @CopyID AND Status <> 'Checked Out';
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            var bookId = await GetBookIdForCopyAsync(connection, transaction, copyId);
            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@CopyID", SqlDbType.Int).Value = copyId;
            var deleted = await command.ExecuteNonQueryAsync() == 1;
            if (deleted && bookId.HasValue) await RefreshInventoryAsync(connection, transaction, bookId.Value);
            await transaction.CommitAsync();
            return deleted;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> GetCopyCountByStatusAsync(int bookId, string status)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.BookCopies WHERE BookID = @BookID AND Status = @Status;";
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = status;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public Task<List<BookCopy>> GetCopiesNeedingAttentionAsync() =>
        ReadCopiesAsync("WHERE Status IN ('Damaged', 'Lost') ORDER BY DateModified DESC");

    private async Task<List<BookCopy>> ReadCopiesAsync(
        string predicate,
        params (string Name, object Value)[] parameters)
    {
        string sql = $"""
            SELECT CopyID, BookID, Barcode, Condition, Status, Location,
                   CurrentReservationID, CheckoutDueDate, DateAcquired, Notes, DateModified
            FROM dbo.BookCopies
            {predicate};
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters)
            AddInferredParameter(command, parameter.Name, parameter.Value);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<BookCopy>();
        while (await reader.ReadAsync()) result.Add(ReadCopy(reader));
        return result;
    }

    private async Task<bool> UpdateSingleFieldAsync(int copyId, string field, object? value)
    {
        var allowedFields = new[] { "Status", "Condition", "Location" };
        if (!allowedFields.Contains(field)) throw new ArgumentException("Invalid copy field.", nameof(field));
        string sql = $"""
            UPDATE dbo.BookCopies
            SET {field} = @Value, DateModified = SYSUTCDATETIME()
            WHERE CopyID = @CopyID;
            """;
        return await UpdateWithInventoryAsync(sql, copyId, ("@Value", value ?? DBNull.Value));
    }

    private async Task<bool> UpdateWithInventoryAsync(
        string sql,
        int copyId,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            var bookId = await GetBookIdForCopyAsync(connection, transaction, copyId);
            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@CopyID", SqlDbType.Int).Value = copyId;
            foreach (var parameter in parameters)
            {
                AddInferredParameter(command, parameter.Name, parameter.Value);
            }
            var updated = await command.ExecuteNonQueryAsync() == 1;
            if (updated && bookId.HasValue) await RefreshInventoryAsync(connection, transaction, bookId.Value);
            await transaction.CommitAsync();
            return updated;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<int?> GetBookIdForCopyAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int copyId)
    {
        const string sql = "SELECT BookID FROM dbo.BookCopies WHERE CopyID = @CopyID;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@CopyID", SqlDbType.Int).Value = copyId;
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? null : Convert.ToInt32(value);
    }

    private static async Task RefreshInventoryAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int bookId)
    {
        const string sql = """
            UPDATE b
            SET TotalCopies = x.TotalCopies,
                CopiesAvailable = x.AvailableCopies
            FROM dbo.Books b
            CROSS APPLY
            (
                SELECT COUNT(*) AS TotalCopies,
                       COALESCE(SUM(CASE WHEN Status = 'Available' THEN 1 ELSE 0 END), 0) AS AvailableCopies
                FROM dbo.BookCopies
                WHERE BookID = @BookID
            ) x
            WHERE b.BookID = @BookID;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
        await command.ExecuteNonQueryAsync();
    }

    private static BookCopy ReadCopy(SqlDataReader reader) =>
        new()
        {
            CopyId = reader.GetInt32(0),
            BookId = reader.GetInt32(1),
            Barcode = NullableString(reader, 2),
            Condition = NullableString(reader, 3) ?? "Good",
            Status = NullableString(reader, 4) ?? "Available",
            Location = NullableString(reader, 5),
            CurrentReservationId = NullableInt(reader, 6),
            CheckoutDueDate = NullableDateTime(reader, 7),
            DateAcquired = reader.GetDateTime(8),
            Notes = NullableString(reader, 9),
            DateModified = reader.GetDateTime(10)
        };

    private static void AddNullable(SqlCommand command, string name, SqlDbType type, int? size, object? value)
    {
        var parameter = size.HasValue
            ? command.Parameters.Add(name, type, size.Value)
            : command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }

    private static void AddInferredParameter(SqlCommand command, string name, object? value)
    {
        if (name == "@Value")
        {
            command.Parameters.Add(name, SqlDbType.NVarChar, 255).Value = value ?? DBNull.Value;
            return;
        }
        SqlDbType type = value switch
        {
            int or DBNull => SqlDbType.Int,
            DateTime => SqlDbType.DateTime2,
            _ => SqlDbType.NVarChar
        };
        command.Parameters.Add(name, type).Value = value ?? DBNull.Value;
    }

    private static string? NullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? NullableInt(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static DateTime? NullableDateTime(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
}
