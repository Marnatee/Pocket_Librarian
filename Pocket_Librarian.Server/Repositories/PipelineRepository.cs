//Data access layer (SQL Database)
//This would communicate with the SQL Database.
//SQL Database is made in SQL Server Management Studio (SSMS).
using Microsoft.Data.SqlClient;


namespace Pocket_Librarian.Server.Repositories
{
    public class PipelineRepository
    {
        private readonly string _connectionString;

        public PipelineRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PocketLibrarianDatabase")
                ?? throw new InvalidOperationException(
                    "Connection string 'PocketLibrarianDatabase' was not found.");
        }
        public string TestDataAccess()
        {
            try {
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

                    return "Books found: " + string.Join(", ", bookTitles);
            }
            catch (SqlException)
            {
                return "Database connection failed. The database could not be reached.";
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
