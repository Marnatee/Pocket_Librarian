using Microsoft.Data.SqlClient;
using Pocket_Librarian.Server.Models;

namespace Pocket_Librarian.Server.Repositories;

public class DonationRepository
{
    private readonly IConfiguration _configuration;

    public DonationRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Retrieves the database connection string from appsettings.json.
    private string ConnectionString =>
        _configuration.GetConnectionString(
            "PocketLibrarianDatabase")
        ?? throw new InvalidOperationException(
            "Database connection string was not found.");

    // Creates the donations table if it does not already exist.
    public async Task EnsureDonationsTableAsync()
    {
        const string sql = """
            IF OBJECT_ID('dbo.BookDonations', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.BookDonations
                (
                    DonationID INT IDENTITY(1,1) PRIMARY KEY,

                    -- Connects the donation to the member who submitted it.
                    UserID INT NOT NULL,

                    Title NVARCHAR(255) NOT NULL,
                    Author NVARCHAR(255) NOT NULL,
                    ISBN NVARCHAR(30) NULL,
                    Genre NVARCHAR(100) NOT NULL,
                    PublicationYear INT NULL,
                    Description NVARCHAR(MAX) NULL,
                    BookCondition NVARCHAR(100) NOT NULL,
                    NumberOfCopies INT NOT NULL,

                    -- Optional donation information.
                    CoverImageUrl NVARCHAR(500) NULL,
                    DonationComments NVARCHAR(MAX) NULL,

                    -- New donations begin awaiting staff verification.
                    Status NVARCHAR(50) NOT NULL
                        DEFAULT 'Pending Verification',

                    SubmittedAt DATETIME2 NOT NULL
                        DEFAULT SYSDATETIME(),

                    -- Links the donation to the member account.
                    CONSTRAINT FK_BookDonations_Users
                        FOREIGN KEY (UserID)
                        REFERENCES dbo.[Library Table](id)
                );
            END
            """;

        await using var connection =
            new SqlConnection(ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        await command.ExecuteNonQueryAsync();
    }

    // Inserts a donation and attaches it to the authenticated member.
    public async Task<int> SubmitDonationAsync(
        string username,
        CreateDonationRequest request)
    {
        const string sql = """
            INSERT INTO dbo.BookDonations
            (
                UserID,
                Title,
                Author,
                ISBN,
                Genre,
                PublicationYear,
                Description,
                BookCondition,
                NumberOfCopies,
                CoverImageUrl,
                DonationComments,
                Status
            )
            OUTPUT INSERTED.DonationID
            SELECT
                id,
                @Title,
                @Author,
                @ISBN,
                @Genre,
                @PublicationYear,
                @Description,
                @BookCondition,
                @NumberOfCopies,
                @CoverImageUrl,
                @DonationComments,
                'Pending Verification'
            FROM dbo.[Library Table]
            WHERE username = @Username;
            """;

        await using var connection =
            new SqlConnection(ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        // Identifies the member's account.
        command.Parameters.AddWithValue(
            "@Username",
            username);

        // Required donation information.
        command.Parameters.AddWithValue(
            "@Title",
            request.Title);

        command.Parameters.AddWithValue(
            "@Author",
            request.Author);

        command.Parameters.AddWithValue(
            "@Genre",
            request.Genre);

        command.Parameters.AddWithValue(
            "@BookCondition",
            request.BookCondition);

        command.Parameters.AddWithValue(
            "@NumberOfCopies",
            request.NumberOfCopies);

        // Optional values are stored as NULL when not provided.
        command.Parameters.AddWithValue(
            "@ISBN",
            (object?)request.ISBN ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@PublicationYear",
            (object?)request.PublicationYear ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@Description",
            (object?)request.Description ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@CoverImageUrl",
            (object?)request.CoverImageUrl ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@DonationComments",
            (object?)request.DonationComments ?? DBNull.Value);

        // Returns the newly created donation ID.
        object? result =
            await command.ExecuteScalarAsync();

        if (result == null)
        {
            throw new InvalidOperationException(
                "The member account could not be found.");
        }

        return Convert.ToInt32(result);
    }
}