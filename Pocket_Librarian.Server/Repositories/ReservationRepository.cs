// Data access layer (SQL Database)
// This layer communicates with the SQL Database to perform CRUD operations on reservations.
using Microsoft.Data.SqlClient;
using Pocket_Librarian.Server.Models;

namespace Pocket_Librarian.Server.Repositories
{
    public class ReservationRepository
    {
        private const string ReservationSelectSql = @"
            SELECT r.ReservationID, r.UserID, r.BookID, r.DateReserved, r.DateReturned,
                   CONCAT(u.FirstName, ' ', u.LastName) AS UserName,
                   u.Email AS UserEmail,
                   b.Title AS BookTitle
            FROM dbo.Reservations AS r
            INNER JOIN dbo.Users AS u ON u.UserID = r.UserID
            INNER JOIN dbo.Books AS b ON b.BookID = r.BookID";

        private readonly string _configuredConnectionString;
        private string? _manualConnectionString;

        public ReservationRepository(IConfiguration configuration)
        {
            _configuredConnectionString = configuration.GetConnectionString("PocketLibrarianDatabase")
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

        
        public async Task<int> CreateReservationAsync(Reservation reservation)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    using SqlTransaction transaction = connection.BeginTransaction();

                    string duplicateQuery = @"
                        SELECT COUNT(1)
                        FROM dbo.Reservations WITH (UPDLOCK, HOLDLOCK)
                        WHERE UserID = @UserId
                          AND BookID = @BookId
                          AND DateReturned IS NULL";

                    using (SqlCommand duplicateCommand = new SqlCommand(
                        duplicateQuery, connection, transaction))
                    {
                        duplicateCommand.Parameters.AddWithValue("@UserId", reservation.UserId);
                        duplicateCommand.Parameters.AddWithValue("@BookId", reservation.BookId);

                        int duplicateCount = Convert.ToInt32(
                            await duplicateCommand.ExecuteScalarAsync());

                        if (duplicateCount > 0)
                        {
                            throw new InvalidOperationException(
                                "This user already has this book checked out.");
                        }
                    }

                    string availabilityQuery = @"
                        SELECT CopiesAvailable
                        FROM dbo.Books WITH (UPDLOCK, ROWLOCK)
                        WHERE BookID = @BookId";

                    int copiesAvailable;
                    using (SqlCommand availabilityCommand = new SqlCommand(
                        availabilityQuery, connection, transaction))
                    {
                        availabilityCommand.Parameters.AddWithValue("@BookId", reservation.BookId);
                        object? result = await availabilityCommand.ExecuteScalarAsync();

                        if (result is null || result == DBNull.Value)
                        {
                            throw new InvalidOperationException("The requested book does not exist.");
                        }

                        copiesAvailable = Convert.ToInt32(result);
                    }

                    if (copiesAvailable <= 0)
                    {
                        throw new InvalidOperationException(
                            "No copies of this book are currently available.");
                    }

                    string decrementQuery = @"
                        UPDATE dbo.Books
                        SET CopiesAvailable = CopiesAvailable - 1
                        WHERE BookID = @BookId AND CopiesAvailable > 0";

                    using (SqlCommand decrementCommand = new SqlCommand(
                        decrementQuery, connection, transaction))
                    {
                        decrementCommand.Parameters.AddWithValue("@BookId", reservation.BookId);

                        if (await decrementCommand.ExecuteNonQueryAsync() != 1)
                        {
                            throw new InvalidOperationException(
                                "The book became unavailable before checkout completed.");
                        }
                    }

                    string query = @"
                        INSERT INTO dbo.Reservations (UserID, BookID, DateReserved, DateReturned)
                        VALUES (@UserId, @BookId, @DateReserved, @DateReturned);
                        SELECT CAST(SCOPE_IDENTITY() AS int);";

                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@UserId", reservation.UserId);
                        command.Parameters.AddWithValue("@BookId", reservation.BookId);
                        command.Parameters.AddWithValue("@DateReserved", reservation.DateReserved);
                        command.Parameters.AddWithValue("@DateReturned", reservation.DateReturned ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();
                        transaction.Commit();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error creating reservation in database", ex);
            }
        }

        
        public async Task<Reservation?> GetReservationByIdAsync(int reservationId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = ReservationSelectSql + @"
                        WHERE r.ReservationID = @ReservationId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReservationId", reservationId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapReservation(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving reservation from database", ex);
            }

            return null;
        }

        
        public async Task<List<Reservation>> GetReservationsByUserIdAsync(int userId)
        {
            var reservations = new List<Reservation>();

            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = ReservationSelectSql + @"
                        WHERE r.UserID = @UserId
                        ORDER BY r.DateReserved DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reservations.Add(MapReservation(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving user reservations from database", ex);
            }

            return reservations;
        }

        
        public async Task<List<Reservation>> GetReservationsByBookIdAsync(int bookId)
        {
            var reservations = new List<Reservation>();

            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = ReservationSelectSql + @"
                        WHERE r.BookID = @BookId
                        ORDER BY r.DateReserved DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BookId", bookId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reservations.Add(MapReservation(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving book reservations from database", ex);
            }

            return reservations;
        }

        
        public async Task<List<Reservation>> GetActiveReservationsAsync()
        {
            var reservations = new List<Reservation>();

            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = ReservationSelectSql + @"
                        WHERE r.DateReturned IS NULL
                        ORDER BY r.DateReserved ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reservations.Add(MapReservation(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving active reservations from database", ex);
            }

            return reservations;
        }

        
        public async Task<bool> UpdateReservationAsync(Reservation reservation)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                        UPDATE Reservations
                        SET UserId = @UserId, BookId = @BookId, DateReserved = @DateReserved, DateReturned = @DateReturned
                        WHERE ReservationId = @ReservationId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReservationId", reservation.ReservationId);
                        command.Parameters.AddWithValue("@UserId", reservation.UserId);
                        command.Parameters.AddWithValue("@BookId", reservation.BookId);
                        command.Parameters.AddWithValue("@DateReserved", reservation.DateReserved);
                        command.Parameters.AddWithValue("@DateReturned", reservation.DateReturned ?? (object)DBNull.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error updating reservation in database", ex);
            }
        }

        public async Task<bool> ReturnReservationAsync(int reservationId)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(GetActiveConnectionString());
                await connection.OpenAsync();

                using SqlTransaction transaction = connection.BeginTransaction();

                const string findBookQuery = @"
                    SELECT BookID
                    FROM dbo.Reservations WITH (UPDLOCK, ROWLOCK)
                    WHERE ReservationID = @ReservationID
                      AND DateReturned IS NULL";

                int bookId;
                using (SqlCommand findBookCommand = new SqlCommand(
                    findBookQuery, connection, transaction))
                {
                    findBookCommand.Parameters.AddWithValue("@ReservationID", reservationId);
                    object? result = await findBookCommand.ExecuteScalarAsync();

                    if (result is null || result == DBNull.Value)
                    {
                        return false;
                    }

                    bookId = Convert.ToInt32(result);
                }

                const string returnQuery = @"
                    UPDATE dbo.Reservations
                    SET DateReturned = @DateReturned
                    WHERE ReservationID = @ReservationID
                      AND DateReturned IS NULL";

                using (SqlCommand returnCommand = new SqlCommand(
                    returnQuery, connection, transaction))
                {
                    returnCommand.Parameters.AddWithValue("@ReservationID", reservationId);
                    returnCommand.Parameters.AddWithValue("@DateReturned", DateTime.UtcNow);

                    if (await returnCommand.ExecuteNonQueryAsync() != 1)
                    {
                        return false;
                    }
                }

                const string incrementQuery = @"
                    UPDATE dbo.Books
                    SET CopiesAvailable = CASE
                        WHEN CopiesAvailable < TotalCopies THEN CopiesAvailable + 1
                        ELSE TotalCopies
                    END
                    WHERE BookID = @BookID";

                using (SqlCommand incrementCommand = new SqlCommand(
                    incrementQuery, connection, transaction))
                {
                    incrementCommand.Parameters.AddWithValue("@BookID", bookId);

                    if (await incrementCommand.ExecuteNonQueryAsync() != 1)
                    {
                        throw new InvalidOperationException(
                            "The reservation was returned, but the book inventory could not be updated.");
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error returning reservation in database", ex);
            }
        }

        public async Task<bool> IsBookAvailableAsync(int bookId)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(GetActiveConnectionString());
                await connection.OpenAsync();

                const string query = @"
                    SELECT CopiesAvailable
                    FROM dbo.Books
                    WHERE BookID = @BookID";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@BookID", bookId);
                object? result = await command.ExecuteScalarAsync();

                if (result is null || result == DBNull.Value)
                {
                    throw new InvalidOperationException("The requested book does not exist.");
                }

                return Convert.ToInt32(result) > 0;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error checking book availability in database", ex);
            }
        }

        public async Task<bool> DeleteReservationAsync(int reservationId)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(GetActiveConnectionString());
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                const string findReservationQuery = @"
                    SELECT BookID, DateReturned
                    FROM dbo.Reservations WITH (UPDLOCK, ROWLOCK)
                    WHERE ReservationID = @ReservationID";

                int bookId;
                bool isActive;
                using (SqlCommand findReservationCommand = new SqlCommand(
                    findReservationQuery, connection, transaction))
                {
                    findReservationCommand.Parameters.AddWithValue("@ReservationID", reservationId);

                    using SqlDataReader reader = await findReservationCommand.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        return false;
                    }

                    bookId = reader.GetInt32(0);
                    isActive = reader.IsDBNull(1);
                }

                const string deleteQuery = @"
                    DELETE FROM dbo.Reservations
                    WHERE ReservationID = @ReservationID";

                using (SqlCommand deleteCommand = new SqlCommand(
                    deleteQuery, connection, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@ReservationID", reservationId);

                    if (await deleteCommand.ExecuteNonQueryAsync() != 1)
                    {
                        return false;
                    }
                }

                if (isActive)
                {
                    const string incrementQuery = @"
                        UPDATE dbo.Books
                        SET CopiesAvailable = CASE
                            WHEN CopiesAvailable < TotalCopies THEN CopiesAvailable + 1
                            ELSE TotalCopies
                        END
                        WHERE BookID = @BookID";

                    using SqlCommand incrementCommand = new SqlCommand(
                        incrementQuery, connection, transaction);
                    incrementCommand.Parameters.AddWithValue("@BookID", bookId);

                    if (await incrementCommand.ExecuteNonQueryAsync() != 1)
                    {
                        throw new InvalidOperationException(
                            "The reservation was deleted, but the book inventory could not be updated.");
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting reservation from database", ex);
            }
        }

       
        private static Reservation MapReservation(SqlDataReader reader)
        {
            return new Reservation
            {
                ReservationId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                BookId = reader.GetInt32(2),
                DateReserved = reader.GetDateTime(3),
                DateReturned = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                UserName = reader.IsDBNull(5) ? null : reader.GetString(5),
                UserEmail = reader.IsDBNull(6) ? null : reader.GetString(6),
                BookTitle = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
        }
    }
}
