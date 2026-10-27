//Data access layer (SQL Database)
//This would communicate with the SQL Database.
//SQL Database is made in SQL Server Management Studio (SSMS).
using Microsoft.Data.SqlClient;


namespace Pocket_Librarian.Server.Repositories
{
    public class PipelineRepository
    {
        private const string MaskedConnectionDetails =
            @"Server=LEMON-••••••\SQLEXPRESS; Database=Pocket-••••••••-DB";

        private readonly string _connectionString;

        public PipelineRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PocketLibrarianDatabase")
                ?? string.Empty;
        }

        public (bool Connected, string Message, string Details) TestDataAccess()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                return (
                    false,
                    "Database connection failed. No database connection string is configured.",
                    MaskedConnectionDetails
                );
            }

            try
            {
                List<string> bookTitles = new List<string>();

                using SqlConnection connection = new SqlConnection(_connectionString);

                string query = "SELECT Title FROM dbo.Books;";

                using SqlCommand command = new SqlCommand(query, connection);

                connection.Open();

                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    bookTitles.Add(reader["Title"].ToString()!);
                }

                return (
                    true,
                    "Books found: " + string.Join(", ", bookTitles),
                    MaskedConnectionDetails
                );
            }
            catch (SqlException)
            {
                return (
                    false,
                    "Database connection failed. The database could not be reached.",
                    MaskedConnectionDetails
                );
            }
            catch (Exception)
            {
                return (
                    false,
                    "Database connection failed. An unexpected database error occurred.",
                    MaskedConnectionDetails
                );
            }

            /*
            //A real repository would query the SQLdatabase here.
            //For now, it simply acknowledges that it was reached.
            return "Data-access layer received the request successfully.";
            //Replace this line later with Entity Framework or SQL operation
            */
        }
    }
}
