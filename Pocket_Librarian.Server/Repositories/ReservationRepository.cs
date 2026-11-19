using Microsoft.Data.SqlClient;
using Pocket_Librarian.Server.Models;

namespace Pocket_Librarian.Server.Repositories;

// Handles database operations for reservations
public class ReservationRepository
{
    // Provides access to appsettings.json
    private readonly IConfiguration _configuration;

    // Receives configuration through dependency injection
    public ReservationRepository(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Retrieves current reservations for one member
    public async Task<List<Reservation>>
        GetCurrentReservationsAsync(string username)
    {
        // Retrieves the database connection string
        string connectionString =
            _configuration.GetConnectionString(
                "PocketLibrarianDatabase")!;

        // Opens a connection to SQL Server
        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync();

        // Creates the Reservations table if it does not exist
        string createTableSql = @"
            IF OBJECT_ID('dbo.Reservations', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Reservations
                (
                    ReservationID INT IDENTITY(1,1)
                        PRIMARY KEY,

                    UserID INT NOT NULL,

                    BookTitle NVARCHAR(200) NOT NULL,

                    ReservationDate DATETIME2 NOT NULL
                        DEFAULT SYSDATETIME(),

                    Status NVARCHAR(20) NOT NULL
                        DEFAULT 'Current',

                    CONSTRAINT FK_Reservations_LibraryTable
                        FOREIGN KEY (UserID)
                        REFERENCES dbo.[Library Table](id)
                );
            END;";

        // Sends the table-creation command to SQL Server
        await using var createCommand =
            new SqlCommand(
                createTableSql,
                connection);

        await createCommand.ExecuteNonQueryAsync();

        // Selects current reservations belonging to the member
        string selectSql = @"
            SELECT
                r.ReservationID,
                r.UserID,
                r.BookTitle,
                r.ReservationDate,
                r.Status
            FROM dbo.Reservations r
            INNER JOIN dbo.[Library Table] u
                ON r.UserID = u.id
            WHERE u.username = @Username
              AND r.Status = 'Current'
            ORDER BY r.ReservationDate DESC;";

        // Creates the reservation query command
        await using var selectCommand =
            new SqlCommand(
                selectSql,
                connection);

        // Safely supplies the member's username
        selectCommand.Parameters.AddWithValue(
            "@Username",
            username);

        // Executes the query and reads the results
        await using var reader =
            await selectCommand.ExecuteReaderAsync();

        // Stores all reservations found for the member
        List<Reservation> reservations = new();

        // Reads each reservation returned by SQL Server
        while (await reader.ReadAsync())
        {
            // Converts one database row into a Reservation object
            reservations.Add(new Reservation
            {
                ReservationId =
                    Convert.ToInt32(
                        reader["ReservationID"]),

                UserId =
                    Convert.ToInt32(
                        reader["UserID"]),

                BookTitle =
                    reader["BookTitle"]?.ToString() ?? "",

                ReservationDate =
                    Convert.ToDateTime(
                        reader["ReservationDate"]),

                Status =
                    reader["Status"]?.ToString() ?? ""
            });
        }

        // Returns the member's reservations
        return reservations;
    }
}