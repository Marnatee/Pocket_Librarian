using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;
using System.Data;

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

        // Searches for books that match all search criteria supplied by the user.
        // Empty criteria are ignored so any combination of filters can be used.
        public List<CatalogueBook> SearchBooks(CatalogueSearchCriteria criteria)
        {
            List<CatalogueBook> books = new List<CatalogueBook>();

            const string sql = """
                SELECT
                    b.BookID,
                    b.ISBN,
                    b.Title,
                    b.Description,
                    b.TotalCopies,
                    b.CopiesAvailable
                FROM dbo.Books b
                WHERE
                    (@Title = '' OR b.Title LIKE '%' + @Title + '%')
                    AND (@ISBN = '' OR b.ISBN LIKE '%' + @ISBN + '%')
                    AND (
                        @Author = ''
                        OR EXISTS
                        (
                            SELECT 1
                            FROM dbo.BookAuthors ba
                            INNER JOIN dbo.Authors a
                                ON ba.AuthorID = a.AuthorID
                            WHERE ba.BookID = b.BookID
                                AND CONCAT(a.FirstName, ' ', a.LastName)
                                    LIKE '%' + @Author + '%'
                        )
                    )
                    AND (
                        @GenreId IS NULL
                        OR EXISTS
                        (
                            SELECT 1
                            FROM dbo.BookGenres bg
                            WHERE bg.BookID = b.BookID
                                AND bg.GenreID = @GenreId
                        )
                    )
                    AND (@AvailableOnly = 0 OR b.CopiesAvailable > 0)
                ORDER BY b.Title;
                """;

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Title", criteria.Title);
            command.Parameters.AddWithValue("@ISBN", criteria.ISBN);
            command.Parameters.AddWithValue("@Author", criteria.Author);

            SqlParameter genreParameter =
                command.Parameters.Add("@GenreId", SqlDbType.Int);

            genreParameter.Value =
                criteria.GenreId.HasValue
                    ? criteria.GenreId.Value
                    : DBNull.Value;

            command.Parameters.AddWithValue(
                "@AvailableOnly",
                criteria.AvailableOnly);

            connection.Open();

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    books.Add(new CatalogueBook
                    {
                        BookId = reader.GetInt32(reader.GetOrdinal("BookID")),

                        ISBN = reader.IsDBNull(reader.GetOrdinal("ISBN"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("ISBN")),

                        Title = reader.GetString(reader.GetOrdinal("Title")),

                        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Description")),

                        TotalCopies =
                            reader.GetInt32(reader.GetOrdinal("TotalCopies")),

                        CopiesAvailable =
                            reader.GetInt32(reader.GetOrdinal("CopiesAvailable"))
                    });
                }
            }

            // Load each matching book's associated authors and genres.
            foreach (CatalogueBook book in books)
            {
                book.Authors = GetBookAuthors(connection, book.BookId);
                book.Genres = GetBookGenres(connection, book.BookId);
            }

            return books;
        }

        // Retrieves the authors associated with a specific book.
        private List<string> GetBookAuthors(
            SqlConnection connection,
            int bookId)
        {
            List<string> authors = new List<string>();

            const string sql = """
                SELECT CONCAT(a.FirstName, ' ', a.LastName) AS AuthorName
                FROM dbo.Authors a
                INNER JOIN dbo.BookAuthors ba
                    ON a.AuthorID = ba.AuthorID
                WHERE ba.BookID = @BookId
                ORDER BY a.LastName, a.FirstName;
                """;

            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BookId", bookId);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                authors.Add(reader.GetString(reader.GetOrdinal("AuthorName")));
            }

            return authors;
        }

        // Retrieves the genres associated with a specific book.
        private List<string> GetBookGenres(
            SqlConnection connection,
            int bookId)
        {
            List<string> genres = new List<string>();

            const string sql = """
                SELECT g.GenreName
                FROM dbo.Genres g
                INNER JOIN dbo.BookGenres bg
                    ON g.GenreID = bg.GenreID
                WHERE bg.BookID = @BookId
                ORDER BY g.GenreName;
                """;

            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@BookId", bookId);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                genres.Add(reader.GetString(reader.GetOrdinal("GenreName")));
            }

            return genres;
        }
    }
}