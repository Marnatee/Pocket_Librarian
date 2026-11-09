// Data access layer (SQL Database)
// This layer communicates with the SQL Database to perform CRUD operations on reservations.
using Microsoft.Data.SqlClient;
using Pocket_Librarian.Server.Models;

namespace Pocket_Librarian.Server.Repositories
{
    public class ReservationRepository
    {
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

                    string query = @"
                        INSERT INTO Reservations (UserId, BookId, DateReserved, DateReturned)
                        VALUES (@UserId, @BookId, @DateReserved, @DateReturned);
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", reservation.UserId);
                        command.Parameters.AddWithValue("@BookId", reservation.BookId);
                        command.Parameters.AddWithValue("@DateReserved", reservation.DateReserved);
                        command.Parameters.AddWithValue("@DateReturned", reservation.DateReturned ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
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

                    string query = @"
                        SELECT ReservationId, UserId, BookId, DateReserved, DateReturned
                        FROM Reservations
                        WHERE ReservationId = @ReservationId";

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

                    string query = @"
                        SELECT ReservationId, UserId, BookId, DateReserved, DateReturned
                        FROM Reservations
                        WHERE UserId = @UserId
                        ORDER BY DateReserved DESC";

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

                    string query = @"
                        SELECT ReservationId, UserId, BookId, DateReserved, DateReturned
                        FROM Reservations
                        WHERE BookId = @BookId
                        ORDER BY DateReserved DESC";

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

                    string query = @"
                        SELECT ReservationId, UserId, BookId, DateReserved, DateReturned
                        FROM Reservations
                        WHERE DateReturned IS NULL
                        ORDER BY DateReserved ASC";

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

        
        public async Task<bool> DeleteReservationAsync(int reservationId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(GetActiveConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = "DELETE FROM Reservations WHERE ReservationId = @ReservationId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReservationId", reservationId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
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
                DateReturned = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
            };
        }
    }
}
