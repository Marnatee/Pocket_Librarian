using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.DAL.Repositories;

public sealed class DonationRepository : IDonationRepository
{
    private readonly string _connectionString;

    public DonationRepository(IConfiguration configuration)
    {
        _connectionString = configuration["ConnectionStrings:PocketLibrarianDatabase"] ?? string.Empty;
    }

    public Task<List<Donation>> GetPendingDonationsAsync() =>
        GetDonationsByStatusesAsync(["Pending", "Verified"]);

    public Task<List<Donation>> GetApprovedDonationsAsync() =>
        GetDonationsByStatusesAsync(["Approved"]);

    public Task<List<Donation>> GetDonationsByStatusAsync(string status) =>
        GetDonationsByStatusesAsync([NormalizeStatus(status)]);

    public async Task<Donation?> GetDonationByIdAsync(int donationId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return await ReadDonationAsync(connection, null, donationId);
    }

    public async Task<int> CreateDonationAsync(Donation donation)
    {
        const string sql = """
            INSERT INTO dbo.BookDonations
            (
                UserID, Title, Author, ISBN, Genre, PublicationYear, Description,
                BookCondition, NumberOfCopies, CoverImagePath, SourceType,
                DonationComments, Status, VerificationStatus
            )
            OUTPUT INSERTED.DonationID
            VALUES
            (
                @UserID, @Title, @Author, @ISBN, @Genre, @PublicationYear, @Description,
                @BookCondition, @NumberOfCopies, @CoverImagePath, @SourceType,
                @DonationComments, 'Pending Verification', 'Pending'
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        AddDonationParameters(command, donation);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<bool> UpdateDonationAsync(Donation donation)
    {
        const string sql = """
            UPDATE dbo.BookDonations
            SET Title = @Title,
                Author = @Author,
                ISBN = @ISBN,
                Genre = @Genre,
                PublicationYear = @PublicationYear,
                Description = @Description,
                BookCondition = @BookCondition,
                NumberOfCopies = @NumberOfCopies,
                CoverImagePath = @CoverImagePath,
                SourceType = @SourceType,
                DonationComments = @DonationComments
            WHERE DonationID = @DonationID
              AND VerificationStatus IN ('Pending', 'Verified');
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@DonationID", SqlDbType.Int).Value = donation.DonationId;
        AddDonationParameters(command, donation, includeId: false);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> UpdateDonationStatusAsync(
        int donationId,
        string newStatus,
        int staffId,
        string? notes = null)
    {
        var status = NormalizeStatus(newStatus);
        const string sql = """
            UPDATE dbo.BookDonations
            SET Status = @DisplayStatus,
                VerificationStatus = @VerificationStatus,
                ReviewedByUserID = @StaffID,
                ReviewedAt = SYSUTCDATETIME(),
                ReviewNotes = @Notes
            WHERE DonationID = @DonationID
              AND VerificationStatus IN ('Pending', 'Verified');
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@DisplayStatus", SqlDbType.NVarChar, 50).Value = DisplayStatus(status);
        command.Parameters.Add("@VerificationStatus", SqlDbType.NVarChar, 50).Value = status;
        command.Parameters.Add("@StaffID", SqlDbType.Int).Value = staffId;
        command.Parameters.Add("@DonationID", SqlDbType.Int).Value = donationId;
        AddNullable(command, "@Notes", SqlDbType.NVarChar, -1, notes);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> ApproveDonationAsync(
        int donationId,
        int? existingBookId,
        int staffId,
        string? notes = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            var donation = await ReadDonationAsync(connection, transaction, donationId, pendingOnly: true)
                ?? throw new InvalidOperationException("The donation is no longer pending verification.");

            int bookId = existingBookId ?? await CreateBookAsync(connection, transaction, donation);
            if (existingBookId.HasValue && !await BookExistsAsync(connection, transaction, existingBookId.Value))
                throw new InvalidOperationException("The selected catalogue book could not be found.");

            await AddAuthorAndGenreLinksAsync(connection, transaction, bookId, donation.Author, donation.Genre);
            await AddAggregateCopiesAsync(connection, transaction, bookId, donation.CopiesDonated);

            const string reviewSql = """
                UPDATE dbo.BookDonations
                SET Status = 'Approved',
                    VerificationStatus = 'Approved',
                    ReviewedByUserID = @StaffID,
                    ReviewedAt = SYSUTCDATETIME(),
                    ReviewNotes = @Notes,
                    ApprovedBookID = @BookID
                WHERE DonationID = @DonationID
                  AND VerificationStatus IN ('Pending', 'Verified');
                """;
            await using var reviewCommand = new SqlCommand(reviewSql, connection, transaction);
            reviewCommand.Parameters.Add("@StaffID", SqlDbType.Int).Value = staffId;
            reviewCommand.Parameters.Add("@Notes", SqlDbType.NVarChar, -1).Value = (object?)notes ?? DBNull.Value;
            reviewCommand.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            reviewCommand.Parameters.Add("@DonationID", SqlDbType.Int).Value = donationId;

            if (await reviewCommand.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException("The donation could not be approved.");

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> RejectDonationAsync(int donationId, int staffId, string? reason = null) =>
        await SetTerminalStatusAsync(donationId, staffId, "Rejected", reason);

    public async Task<bool> ArchiveDonationAsync(int donationId, int staffId, string? notes = null) =>
        await SetTerminalStatusAsync(donationId, staffId, "Archived", notes);

    public async Task<bool> DeleteDonationAsync(int donationId)
    {
        const string sql = """
            DELETE FROM dbo.BookDonations
            WHERE DonationID = @DonationID
              AND VerificationStatus <> 'Approved';
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@DonationID", SqlDbType.Int).Value = donationId;
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> ISBNExistsAsync(string isbn, int? excludeDonationId = null)
    {
        if (string.IsNullOrWhiteSpace(isbn)) return false;
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM dbo.Books WHERE ISBN = @ISBN
                UNION ALL
                SELECT 1 FROM dbo.BookDonations
                WHERE ISBN = @ISBN
                  AND VerificationStatus NOT IN ('Rejected', 'Archived')
                  AND (@ExcludeID IS NULL OR DonationID <> @ExcludeID)
            ) THEN 1 ELSE 0 END;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ISBN", SqlDbType.NVarChar, 20).Value = isbn.Trim();
        command.Parameters.Add("@ExcludeID", SqlDbType.Int).Value = (object?)excludeDonationId ?? DBNull.Value;
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private async Task<bool> SetTerminalStatusAsync(int donationId, int staffId, string status, string? notes)
    {
        const string sql = """
            UPDATE dbo.BookDonations
            SET Status = @DisplayStatus,
                VerificationStatus = @Status,
                ReviewedByUserID = @StaffID,
                ReviewedAt = SYSUTCDATETIME(),
                ReviewNotes = @Notes
            WHERE DonationID = @DonationID
              AND VerificationStatus <> 'Approved';
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@DisplayStatus", SqlDbType.NVarChar, 50).Value = DisplayStatus(status);
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = status;
        command.Parameters.Add("@StaffID", SqlDbType.Int).Value = staffId;
        command.Parameters.Add("@DonationID", SqlDbType.Int).Value = donationId;
        AddNullable(command, "@Notes", SqlDbType.NVarChar, -1, notes);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private async Task<List<Donation>> GetDonationsByStatusesAsync(string[] statuses)
    {
        var parameterNames = statuses.Select((_, index) => $"@Status{index}").ToArray();
        string sql = $"""
            SELECT d.DonationID, d.UserID, d.Title, d.Author, d.ISBN, d.Genre,
                   d.PublicationYear, d.Description, d.BookCondition, d.NumberOfCopies,
                   d.CoverImagePath, d.SourceType, d.VerificationStatus,
                   d.DonationComments, d.ReviewNotes, d.SubmittedAt,
                   d.ReviewedByUserID, d.ReviewedAt, d.ApprovedBookID,
                   u.FirstName, u.LastName, u.Email
            FROM dbo.BookDonations d
            LEFT JOIN dbo.Users u ON u.UserID = d.UserID
            WHERE d.VerificationStatus IN ({string.Join(", ", parameterNames)})
            ORDER BY d.SubmittedAt ASC;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        for (int i = 0; i < statuses.Length; i++)
            command.Parameters.Add(parameterNames[i], SqlDbType.NVarChar, 50).Value = statuses[i];
        return await ReadDonationsAsync(command);
    }

    private static async Task<List<Donation>> ReadDonationsAsync(SqlCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var donations = new List<Donation>();
        while (await reader.ReadAsync()) donations.Add(ReadDonation(reader));
        return donations;
    }

    private static async Task<Donation?> ReadDonation(
        SqlConnection connection,
        SqlTransaction? transaction,
        int donationId,
        bool pendingOnly = false)
    {
        return await ReadDonationAsync(connection, transaction, donationId, pendingOnly);
    }

    private static async Task<Donation?> ReadDonationAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        int donationId,
        bool pendingOnly = false)
    {
        string pendingClause = pendingOnly
            ? "AND d.VerificationStatus IN ('Pending', 'Verified')"
            : string.Empty;
        string sql = $"""
            SELECT d.DonationID, d.UserID, d.Title, d.Author, d.ISBN, d.Genre,
                   d.PublicationYear, d.Description, d.BookCondition, d.NumberOfCopies,
                   d.CoverImagePath, d.SourceType, d.VerificationStatus,
                   d.DonationComments, d.ReviewNotes, d.SubmittedAt,
                   d.ReviewedByUserID, d.ReviewedAt, d.ApprovedBookID,
                   u.FirstName, u.LastName, u.Email
            FROM dbo.BookDonations d
            LEFT JOIN dbo.Users u ON u.UserID = d.UserID
            WHERE d.DonationID = @DonationID {pendingClause};
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@DonationID", SqlDbType.Int).Value = donationId;
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadDonation(reader) : null;
    }

    private static Donation ReadDonation(SqlDataReader reader)
    {
        string? first = NullableString(reader, 19);
        string? last = NullableString(reader, 20);
        return new Donation
        {
            DonationId = reader.GetInt32(0),
            SubmittedByUserId = NullableInt(reader, 1),
            Title = reader.GetString(2),
            Author = NullableString(reader, 3),
            ISBN = NullableString(reader, 4),
            Genre = NullableString(reader, 5),
            PublicationYear = NullableInt(reader, 6),
            Description = NullableString(reader, 7),
            Condition = NullableString(reader, 8) ?? "Good",
            CopiesDonated = reader.GetInt32(9),
            CoverImagePath = NullableString(reader, 10),
            SourceType = NullableString(reader, 11) ?? "Member donation",
            VerificationStatus = NullableString(reader, 12) ?? "Pending",
            DonationComments = NullableString(reader, 13),
            StaffNotes = NullableString(reader, 14),
            DateSubmitted = reader.GetDateTime(15),
            ProcessedByStaffId = NullableInt(reader, 16),
            DateProcessed = NullableDateTime(reader, 17),
            BookId = NullableInt(reader, 18),
            DonorName = string.Join(" ", new[] { first, last }.Where(value => !string.IsNullOrWhiteSpace(value))),
            DonorEmail = NullableString(reader, 21)
        };
    }

    private static async Task<int> CreateBookAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Donation donation)
    {
        const string sql = """
            INSERT INTO dbo.Books
                (ISBN, Title, Description, TotalCopies, CopiesAvailable,
                 PublicationYear, ConditionSummary, SourceType)
            OUTPUT INSERTED.BookID
            VALUES
                (@ISBN, @Title, @Description, 0, 0,
                 @PublicationYear, @ConditionSummary, @SourceType);
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        AddNullable(command, "@ISBN", SqlDbType.NVarChar, 20, donation.ISBN);
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = donation.Title;
        AddNullable(command, "@Description", SqlDbType.NVarChar, -1, donation.Description);
        AddNullable(command, "@PublicationYear", SqlDbType.Int, null, donation.PublicationYear);
        AddNullable(command, "@ConditionSummary", SqlDbType.NVarChar, 100, donation.Condition);
        command.Parameters.Add("@SourceType", SqlDbType.NVarChar, 100).Value = donation.SourceType;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<bool> BookExistsAsync(SqlConnection connection, SqlTransaction transaction, int bookId)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Books WHERE BookID = @BookID) THEN 1 ELSE 0 END;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task AddAggregateCopiesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int bookId,
        int count)
    {
        const string sql = """
            UPDATE dbo.Books
            SET TotalCopies = TotalCopies + @Count,
                CopiesAvailable = CopiesAvailable + @Count
            WHERE BookID = @BookID;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
        command.Parameters.Add("@Count", SqlDbType.Int).Value = count;
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException("The catalogue inventory could not be updated.");
    }

    private static async Task AddAuthorAndGenreLinksAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int bookId,
        string? author,
        string? genre)
    {
        if (!string.IsNullOrWhiteSpace(author))
        {
            var parts = author.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = parts.Length > 1 ? string.Join(" ", parts[..^1]) : string.Empty;
            var lastName = parts[^1];
            const string sql = """
                DECLARE @AuthorID INT;
                SELECT TOP 1 @AuthorID = AuthorID FROM dbo.Authors
                WHERE FirstName = @FirstName AND LastName = @LastName;
                IF @AuthorID IS NULL
                BEGIN
                    INSERT INTO dbo.Authors (FirstName, LastName) VALUES (@FirstName, @LastName);
                    SET @AuthorID = CONVERT(INT, SCOPE_IDENTITY());
                END;
                IF NOT EXISTS (SELECT 1 FROM dbo.BookAuthors WHERE BookID = @BookID AND AuthorID = @AuthorID)
                    INSERT INTO dbo.BookAuthors (BookID, AuthorID) VALUES (@BookID, @AuthorID);
                """;
            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = firstName;
            command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = lastName;
            command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await command.ExecuteNonQueryAsync();
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            const string sql = """
                DECLARE @GenreID INT;
                SELECT TOP 1 @GenreID = GenreID FROM dbo.Genres WHERE GenreName = @GenreName;
                IF @GenreID IS NULL
                BEGIN
                    INSERT INTO dbo.Genres (GenreName) VALUES (@GenreName);
                    SET @GenreID = CONVERT(INT, SCOPE_IDENTITY());
                END;
                IF NOT EXISTS (SELECT 1 FROM dbo.BookGenres WHERE BookID = @BookID AND GenreID = @GenreID)
                    INSERT INTO dbo.BookGenres (BookID, GenreID) VALUES (@BookID, @GenreID);
                """;
            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@GenreName", SqlDbType.NVarChar, 100).Value = genre.Trim();
            command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static void AddDonationParameters(SqlCommand command, Donation donation, bool includeId = true)
    {
        if (includeId) command.Parameters.Add("@DonationID", SqlDbType.Int).Value = donation.DonationId;
        AddNullable(command, "@UserID", SqlDbType.Int, null, donation.SubmittedByUserId);
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = donation.Title;
        AddNullable(command, "@Author", SqlDbType.NVarChar, 255, donation.Author);
        AddNullable(command, "@ISBN", SqlDbType.NVarChar, 20, donation.ISBN);
        AddNullable(command, "@Genre", SqlDbType.NVarChar, 100, donation.Genre);
        AddNullable(command, "@PublicationYear", SqlDbType.Int, null, donation.PublicationYear);
        AddNullable(command, "@Description", SqlDbType.NVarChar, -1, donation.Description);
        AddNullable(command, "@BookCondition", SqlDbType.NVarChar, 100, donation.Condition);
        command.Parameters.Add("@NumberOfCopies", SqlDbType.Int).Value = donation.CopiesDonated;
        AddNullable(command, "@CoverImagePath", SqlDbType.NVarChar, 500, donation.CoverImagePath);
        command.Parameters.Add("@SourceType", SqlDbType.NVarChar, 100).Value = donation.SourceType;
        AddNullable(command, "@DonationComments", SqlDbType.NVarChar, -1, donation.DonationComments);
    }

    private static void AddNullable(SqlCommand command, string name, SqlDbType type, int? size, object? value)
    {
        var parameter = size.HasValue
            ? command.Parameters.Add(name, type, size.Value)
            : command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }

    private static string NormalizeStatus(string status) =>
        status == "Pending Verification" ? "Pending" : status;

    private static string DisplayStatus(string status) =>
        status == "Pending" ? "Pending Verification" : status;

    private static string? NullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? NullableInt(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static DateTime? NullableDateTime(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
}
