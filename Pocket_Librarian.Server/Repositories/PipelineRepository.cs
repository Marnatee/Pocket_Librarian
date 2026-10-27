//Data access layer (SQL Database)
//This would communicate with the SQL Database.
//SQL Database is made in SQL Server Management Studio (SSMS).
using Microsoft.Data.SqlClient;


namespace Pocket_Librarian.Server.Repositories
{
    public class PipelineRepository
    {
        private readonly string _configuredConnectionString;
        private string? _manualConnectionString;

        public PipelineRepository(IConfiguration configuration)
        {
            _configuredConnectionString = configuration.GetConnectionString("PocketLibrarianDatabase")
                ?? string.Empty;
        }

        public (bool Connected, string Message, string Details) TestDataAccess()
        {
            string activeConnectionString =
                _manualConnectionString ?? _configuredConnectionString;

            string connectionSource =
                _manualConnectionString is null ? "Configured" : "Manual";

            return TestConnection(activeConnectionString, connectionSource);
        }

        public (bool Connected, string Message, string Details) TestManualDataAccess(
            string connectionString)
        {
            var result = TestConnection(connectionString, "Manual");

            if (result.Connected)
            {
                _manualConnectionString = connectionString;
            }

            return result;
        }

        private static (bool Connected, string Message, string Details) TestConnection(
            string connectionString,
            string connectionSource)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return (
                    false,
                    "Database connection failed. No database connection string is configured.",
                    GetMaskedConnectionDetails(connectionString, connectionSource)
                );
            }

            try
            {
                List<string> bookTitles = new List<string>();

                using SqlConnection connection = new SqlConnection(connectionString);

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
                    GetMaskedConnectionDetails(connectionString, connectionSource)
                );
            }
            catch (SqlException)
            {
                return (
                    false,
                    "Database connection failed. The database could not be reached.",
                    GetMaskedConnectionDetails(connectionString, connectionSource)
                );
            }
            catch (Exception)
            {
                return (
                    false,
                    "Database connection failed. An unexpected database error occurred.",
                    GetMaskedConnectionDetails(connectionString, connectionSource)
                );
            }

            /*
            //A real repository would query the SQLdatabase here.
            //For now, it simply acknowledges that it was reached.
            return "Data-access layer received the request successfully.";
            //Replace this line later with Entity Framework or SQL operation
            */
        }

        private static string GetMaskedConnectionDetails(
            string connectionString,
            string connectionSource)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "Database connection is not configured.";
            }

            try
            {
                SqlConnectionStringBuilder connectionBuilder =
                    new SqlConnectionStringBuilder(connectionString);

                return $"{connectionSource} target: "
                    + $"Server={MaskConnectionPart(connectionBuilder.DataSource)}; "
                    + $"Database={MaskConnectionPart(connectionBuilder.InitialCatalog)}";
            }
            catch (ArgumentException)
            {
                return "Database connection is configured; target details are masked.";
            }
        }

        private static string MaskConnectionPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "not specified";
            }

            if (value.Length <= 4)
            {
                return "••••";
            }

            int visiblePrefixLength = Math.Min(3, value.Length - 2);
            return $"{value[..visiblePrefixLength]}•••{value[^2..]}";
        }
    }
}
