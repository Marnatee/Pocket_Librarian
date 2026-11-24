using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.DAL.Repositories;

public sealed class BookDonationRepository : IBookDonationRepository
{
    private readonly string _connectionString;

    public BookDonationRepository(IConfiguration configuration)
    {
        _connectionString = configuration["ConnectionStrings:PocketLibrarianDatabase"]
            ?? string.Empty;
    }

    public async Task<int> SubmitAsync(BookDonationSubmission submission)
    {
        const string sql = """
            INSERT INTO dbo.BookDonations
            (
                UserID, Title, Author, ISBN, Genre, PublicationYear,
                Description, BookCondition, NumberOfCopies, CoverImage,
                CoverImageContentType, CoverImageFileName, DonationComments, Status
            )
            OUTPUT INSERTED.DonationID
            VALUES
            (
                @UserID, @Title, @Author, @ISBN, @Genre, @PublicationYear,
                @Description, @BookCondition, @NumberOfCopies, @CoverImage,
                @CoverImageContentType, @CoverImageFileName, @DonationComments,
                'Pending Verification'
            );
            """;

        await using SqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(sql, connection);
        AddSubmissionParameters(command, submission);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<List<BookDonation>> GetPendingAsync()
    {
        const string sql = """
            SELECT
                d.DonationID, d.UserID, d.Title, d.Author, d.ISBN, d.Genre,
                d.PublicationYear, d.Description, d.BookCondition,
                d.NumberOfCopies, d.CoverImage, d.DonationComments,
                d.Status, d.SubmittedAt, u.FirstName, u.LastName, u.Email
            FROM dbo.BookDonations AS d
            INNER JOIN dbo.Users AS u ON u.UserID = d.UserID
            WHERE d.Status = 'Pending Verification'
            ORDER BY d.SubmittedAt ASC;
            """;

        await using SqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(sql, connection);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();

        List<BookDonation> donations = new();
        while (await reader.ReadAsync())
        {
            donations.Add(new BookDonation
            {
                DonationId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Title = reader.GetString(2),
                Author = NullableString(reader, 3),
                ISBN = NullableString(reader, 4),
                Genre = NullableString(reader, 5),
                PublicationYear = NullableInt(reader, 6),
                Description = NullableString(reader, 7),
                BookCondition = NullableString(reader, 8),
                NumberOfCopies = reader.GetInt32(9),
                HasCoverImage = !reader.IsDBNull(10),
                DonationComments = NullableString(reader, 11),
                Status = reader.GetString(12),
                SubmittedAt = reader.GetDateTime(13),
                DonorName = $"{reader.GetString(14)} {reader.GetString(15)}",
                DonorEmail = reader.GetString(16)
            });
        }

        return donations;
    }

    public async Task<bool> ApproveAsync(
        int donationId,
        int staffUserId,
        int? existingBookId,
        string? reviewNotes)
    {
        await using SqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            DonationRow donation = await GetPendingDonationAsync(
                connection,
                transaction,
                donationId);

            int bookId = existingBookId ?? await CreateBookAsync(
                connection,
                transaction,
                donation);

            if (existingBookId.HasValue)
            {
                await AddCopiesToExistingBookAsync(
                    connection,
                    transaction,
                    existingBookId.Value,
                    donation.NumberOfCopies);
            }

            await AddAuthorAndGenreLinksAsync(
                connection,
                transaction,
                bookId,
                donation.Author,
                donation.Genre);

            const string reviewSql = """
                UPDATE dbo.BookDonations
                SET Status = 'Approved',
                    ReviewedByUserID = @StaffUserID,
                    ReviewedAt = SYSUTCDATETIME(),
                    ReviewNotes = @ReviewNotes,
                    ApprovedBookID = @BookID
                WHERE DonationID = @DonationID
                  AND Status = 'Pending Verification';
                """;

            await using SqlCommand reviewCommand = new(reviewSql, connection, transaction);
            reviewCommand.Parameters.Add("@StaffUserID", SqlDbType.Int).Value = staffUserId;
            reviewCommand.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            reviewCommand.Parameters.Add("@DonationID", SqlDbType.Int).Value = donationId;
            reviewCommand.Parameters.Add("@ReviewNotes", SqlDbType.NVarChar, -1).Value =
                (object?)reviewNotes ?? DBNull.Value;

            bool updated = await reviewCommand.ExecuteNonQueryAsync() == 1;
            if (updated)
            {
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

    public async Task<bool> RejectAsync(int donationId, int staffUserId, string? reviewNotes)
    {
        const string sql = """
            UPDATE dbo.BookDonations
            SET Status = 'Rejected',
                ReviewedByUserID = @StaffUserID,
                ReviewedAt = SYSUTCDATETIME(),
                ReviewNotes = @ReviewNotes
            WHERE DonationID = @DonationID
              AND Status = 'Pending Verification';
            """;

        await using SqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(sql, connection);
        command.Parameters.Add("@StaffUserID", SqlDbType.Int).Value = staffUserId;
        command.Parameters.Add("@DonationID", SqlDbType.Int).Value = donationId;
        command.Parameters.Add("@ReviewNotes", SqlDbType.NVarChar, -1).Value =
            (object?)reviewNotes ?? DBNull.Value;

        return await command.ExecuteNonQueryAsync() == 1;
    }

    private async Task<DonationRow> GetPendingDonationAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int donationId)
    {
        const string sql = """
            SELECT DonationID, Title, Author, ISBN, Genre, PublicationYear,
                   Description, BookCondition, NumberOfCopies, CoverImage,
                   CoverImageContentType, CoverImageFileName
            FROM dbo.BookDonations WITH (UPDLOCK, ROWLOCK)
            WHERE DonationID = @DonationID
              AND Status = 'Pending Verification';
            """;

        await using SqlCommand command = new(sql, connection, transaction);
        command.Parameters.Add("@DonationID", SqlDbType.Int).Value = donationId;
        await using SqlDataReader reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("The donation is no longer pending verification.");
        }

        DonationRow donation = new(
            reader.GetInt32(0),
            reader.GetString(1),
            NullableString(reader, 2),
            NullableString(reader, 3),
            NullableString(reader, 4),
            NullableInt(reader, 5),
            NullableString(reader, 6),
            NullableString(reader, 7),
            reader.GetInt32(8),
            reader.IsDBNull(9) ? null : (byte[])reader[9],
            NullableString(reader, 10),
            NullableString(reader, 11));

        return donation;
    }

    private static async Task<int> CreateBookAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DonationRow donation)
    {
        const string sql = """
            INSERT INTO dbo.Books
                (ISBN, Title, Description, TotalCopies, CopiesAvailable,
                 PublicationYear, ConditionSummary, CoverImage, CoverImageContentType)
            OUTPUT INSERTED.BookID
            VALUES
                (@ISBN, @Title, @Description, @NumberOfCopies, @NumberOfCopies,
                 @PublicationYear, @ConditionSummary, @CoverImage, @CoverImageContentType);
            """;

        await using SqlCommand command = new(sql, connection, transaction);
        AddNullable(command, "@ISBN", SqlDbType.NVarChar, 20, donation.ISBN);
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = donation.Title;
        AddNullable(command, "@Description", SqlDbType.NVarChar, -1, donation.Description);
        command.Parameters.Add("@NumberOfCopies", SqlDbType.Int).Value = donation.NumberOfCopies;
        AddNullable(command, "@PublicationYear", SqlDbType.Int, null, donation.PublicationYear);
        AddNullable(command, "@ConditionSummary", SqlDbType.NVarChar, 100, donation.BookCondition);
        AddNullable(command, "@CoverImage", SqlDbType.VarBinary, -1, donation.CoverImage);
        AddNullable(command, "@CoverImageContentType", SqlDbType.NVarChar, 100, donation.CoverImageContentType);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task AddCopiesToExistingBookAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int bookId,
        int numberOfCopies)
    {
        const string sql = """
            UPDATE dbo.Books
            SET TotalCopies = TotalCopies + @NumberOfCopies,
                CopiesAvailable = CopiesAvailable + @NumberOfCopies
            WHERE BookID = @BookID;
            """;

        await using SqlCommand command = new(sql, connection, transaction);
        command.Parameters.Add("@NumberOfCopies", SqlDbType.Int).Value = numberOfCopies;
        command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;

        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException("The selected catalogue book could not be found.");
        }
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
            string[] nameParts = author.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string firstName = nameParts.Length > 1
                ? string.Join(" ", nameParts[..^1])
                : string.Empty;
            string lastName = nameParts[^1];

            const string authorSql = """
                DECLARE @AuthorID INT;
                SELECT @AuthorID = AuthorID
                FROM dbo.Authors
                WHERE FirstName = @FirstName AND LastName = @LastName;

                IF @AuthorID IS NULL
                BEGIN
                    INSERT INTO dbo.Authors (FirstName, LastName)
                    VALUES (@FirstName, @LastName);
                    SET @AuthorID = CONVERT(INT, SCOPE_IDENTITY());
                END;

                IF NOT EXISTS
                (
                    SELECT 1 FROM dbo.BookAuthors
                    WHERE BookID = @BookID AND AuthorID = @AuthorID
                )
                BEGIN
                    INSERT INTO dbo.BookAuthors (BookID, AuthorID)
                    VALUES (@BookID, @AuthorID);
                END;
                """;

            await using SqlCommand authorCommand = new(authorSql, connection, transaction);
            authorCommand.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = firstName;
            authorCommand.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = lastName;
            authorCommand.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await authorCommand.ExecuteNonQueryAsync();
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            const string genreSql = """
                DECLARE @GenreID INT;
                SELECT @GenreID = GenreID FROM dbo.Genres WHERE GenreName = @GenreName;

                IF @GenreID IS NULL
                BEGIN
                    INSERT INTO dbo.Genres (GenreName) VALUES (@GenreName);
                    SET @GenreID = CONVERT(INT, SCOPE_IDENTITY());
                END;

                IF NOT EXISTS
                (
                    SELECT 1 FROM dbo.BookGenres
                    WHERE BookID = @BookID AND GenreID = @GenreID
                )
                BEGIN
                    INSERT INTO dbo.BookGenres (BookID, GenreID)
                    VALUES (@BookID, @GenreID);
                END;
                """;

            await using SqlCommand genreCommand = new(genreSql, connection, transaction);
            genreCommand.Parameters.Add("@GenreName", SqlDbType.NVarChar, 100).Value = genre.Trim();
            genreCommand.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await genreCommand.ExecuteNonQueryAsync();
        }
    }

    private static void AddSubmissionParameters(SqlCommand command, BookDonationSubmission submission)
    {
        command.Parameters.Add("@UserID", SqlDbType.Int).Value = submission.UserId;
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = submission.Title;
        AddNullable(command, "@Author", SqlDbType.NVarChar, 255, submission.Author);
        AddNullable(command, "@ISBN", SqlDbType.NVarChar, 20, submission.ISBN);
        AddNullable(command, "@Genre", SqlDbType.NVarChar, 100, submission.Genre);
        AddNullable(command, "@PublicationYear", SqlDbType.Int, null, submission.PublicationYear);
        AddNullable(command, "@Description", SqlDbType.NVarChar, -1, submission.Description);
        AddNullable(command, "@BookCondition", SqlDbType.NVarChar, 100, submission.BookCondition);
        command.Parameters.Add("@NumberOfCopies", SqlDbType.Int).Value = submission.NumberOfCopies;
        AddNullable(command, "@CoverImage", SqlDbType.VarBinary, -1, submission.CoverImage);
        AddNullable(command, "@CoverImageContentType", SqlDbType.NVarChar, 100, submission.CoverImageContentType);
        AddNullable(command, "@CoverImageFileName", SqlDbType.NVarChar, 255, submission.CoverImageFileName);
        AddNullable(command, "@DonationComments", SqlDbType.NVarChar, -1, submission.DonationComments);
    }

    private static void AddNullable(
        SqlCommand command,
        string name,
        SqlDbType type,
        int? size,
        object? value)
    {
        SqlParameter parameter = size.HasValue
            ? command.Parameters.Add(name, type, size.Value)
            : command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }

    private static string? NullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? NullableInt(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private sealed record DonationRow(
        int DonationId,
        string Title,
        string? Author,
        string? ISBN,
        string? Genre,
        int? PublicationYear,
        string? Description,
        string? BookCondition,
        int NumberOfCopies,
        byte[]? CoverImage,
        string? CoverImageContentType,
        string? CoverImageFileName);
}
