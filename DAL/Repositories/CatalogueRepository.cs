using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.DAL.Repositories
{
    // Provides SQL Server data access for catalogue-related operations.
    public class CatalogueRepository : ICatalogueRepository
    {
        private readonly string _connectionString;

        // Retrieves the configured Pocket Librarian database connection string.
        public CatalogueRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration["ConnectionStrings:PocketLibrarianDatabase"]
                ?? string.Empty;
        }

        // Retrieves all genres from the database for catalogue filtering.
        public List<CatalogueGenre> GetGenres()
        {
            List<CatalogueGenre> genres = new List<CatalogueGenre>();

            const string sql = """
                SELECT GenreID, GenreName
                FROM dbo.Genres
                ORDER BY GenreName;
                """;

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(sql, connection);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                genres.Add(new CatalogueGenre
                {
                    GenreId = reader.GetInt32(reader.GetOrdinal("GenreID")),
                    GenreName = reader.GetString(reader.GetOrdinal("GenreName"))
                });
            }

            return genres;
        }
    }
}