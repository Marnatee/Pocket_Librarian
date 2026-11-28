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
                    b.CopiesAvailable,
                    b.PublicationYear,
                    b.SourceType
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
                        ,
                        PublicationYear = reader.IsDBNull(reader.GetOrdinal("PublicationYear"))
                            ? null
                            : reader.GetInt32(reader.GetOrdinal("PublicationYear")),
                        SourceType = reader.IsDBNull(reader.GetOrdinal("SourceType"))
                            ? "Library acquisition"
                            : reader.GetString(reader.GetOrdinal("SourceType"))
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

        public async Task<List<CatalogueBook>> GetAllBooksAsync()
        {
            const string sql = """
                SELECT BookID, ISBN, Title, Description, TotalCopies, CopiesAvailable,
                       PublicationYear, SourceType
                FROM dbo.Books
                ORDER BY Title, BookID;
                """;
            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync();
            await using SqlCommand command = new(sql, connection);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<CatalogueBook> books = new();
            while (await reader.ReadAsync()) books.Add(ReadBook(reader));
            await reader.CloseAsync();
            foreach (CatalogueBook book in books)
            {
                book.Authors = await GetBookAuthorsAsync(connection, book.BookId);
                book.Genres = await GetBookGenresAsync(connection, book.BookId);
            }
            return books;
        }

        public async Task<CatalogueBook?> GetBookByIdAsync(int bookId)
        {
            const string sql = """
                SELECT BookID, ISBN, Title, Description, TotalCopies, CopiesAvailable,
                       PublicationYear, SourceType
                FROM dbo.Books
                WHERE BookID = @BookID;
                """;
            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync();
            await using SqlCommand command = new(sql, connection);
            command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            CatalogueBook book = ReadBook(reader);
            await reader.CloseAsync();
            book.Authors = await GetBookAuthorsAsync(connection, bookId);
            book.Genres = await GetBookGenresAsync(connection, bookId);
            return book;
        }

        public async Task<int> CreateBookAsync(CatalogueBookMutation book)
        {
            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync();
            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                const string sql = """
                    INSERT INTO dbo.Books
                        (ISBN, Title, Description, TotalCopies, CopiesAvailable,
                         PublicationYear, SourceType)
                    OUTPUT INSERTED.BookID
                    VALUES
                        (@ISBN, @Title, @Description, @TotalCopies, @CopiesAvailable,
                        @PublicationYear, @SourceType);
                    """;
                await using SqlCommand command = new(sql, connection, transaction);
                AddBookParameters(command, book);
                int bookId = Convert.ToInt32(await command.ExecuteScalarAsync());
                await ReplaceLinksAsync(connection, transaction, bookId, book.Author, book.Genre);
                await transaction.CommitAsync();
                return bookId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateBookAsync(int bookId, CatalogueBookMutation book)
        {
            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync();
            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                const string sql = """
                    UPDATE dbo.Books
                    SET ISBN = @ISBN,
                        Title = @Title,
                        Description = @Description,
                        TotalCopies = @TotalCopies,
                        CopiesAvailable = @CopiesAvailable,
                        PublicationYear = @PublicationYear,
                        SourceType = @SourceType
                    WHERE BookID = @BookID;
                    """;
                await using SqlCommand command = new(sql, connection, transaction);
                command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
                AddBookParameters(command, book);
                bool updated = await command.ExecuteNonQueryAsync() == 1;
                if (updated) await ReplaceLinksAsync(connection, transaction, bookId, book.Author, book.Genre);
                if (updated) await transaction.CommitAsync();
                else await transaction.RollbackAsync();
                return updated;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteBookAsync(int bookId)
        {
            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync();
            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            try
            {
                if (!await BookExistsAsync(connection, transaction, bookId))
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                const string sql = """
                    IF OBJECT_ID(N'dbo.BookDonations', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.BookDonations', N'ApprovedBookID') IS NOT NULL
                        UPDATE dbo.BookDonations
                        SET ApprovedBookID = NULL
                        WHERE ApprovedBookID = @BookID;
                    IF OBJECT_ID(N'dbo.BookCopies', N'U') IS NOT NULL
                        DELETE FROM dbo.BookCopies WHERE BookID = @BookID;
                    DELETE FROM dbo.BookAuthors WHERE BookID = @BookID;
                    DELETE FROM dbo.BookGenres WHERE BookID = @BookID;
                    DELETE FROM dbo.Books WHERE BookID = @BookID;
                    """;
                await using SqlCommand command = new(sql, connection, transaction);
                command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static CatalogueBook ReadBook(SqlDataReader reader) =>
            new()
            {
                BookId = reader.GetInt32(0),
                ISBN = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Title = reader.GetString(2),
                Description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                TotalCopies = reader.GetInt32(4),
                CopiesAvailable = reader.GetInt32(5),
                PublicationYear = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                SourceType = reader.IsDBNull(7) ? "Library acquisition" : reader.GetString(7)
            };

        private static void AddBookParameters(SqlCommand command, CatalogueBookMutation book)
        {
            AddNullable(command, "@ISBN", SqlDbType.NVarChar, 20, book.ISBN);
            command.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = book.Title.Trim();
            AddNullable(command, "@Description", SqlDbType.NVarChar, -1, book.Description);
            command.Parameters.Add("@TotalCopies", SqlDbType.Int).Value = book.TotalCopies;
            command.Parameters.Add("@CopiesAvailable", SqlDbType.Int).Value = book.CopiesAvailable;
            AddNullable(command, "@PublicationYear", SqlDbType.Int, null, book.PublicationYear);
            command.Parameters.Add("@SourceType", SqlDbType.NVarChar, 100).Value = book.SourceType.Trim();
        }

        private static async Task ReplaceLinksAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int bookId,
            string? author,
            string? genre)
        {
            const string deleteSql = """
                DELETE FROM dbo.BookAuthors WHERE BookID = @BookID;
                DELETE FROM dbo.BookGenres WHERE BookID = @BookID;
                """;
            await using (SqlCommand delete = new(deleteSql, connection, transaction))
            {
                delete.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
                await delete.ExecuteNonQueryAsync();
            }
            await AddAuthorAsync(connection, transaction, bookId, author);
            await AddGenreAsync(connection, transaction, bookId, genre);
        }

        private static async Task AddAuthorAsync(SqlConnection connection, SqlTransaction transaction, int bookId, string? author)
        {
            if (string.IsNullOrWhiteSpace(author)) return;
            string[] parts = author.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string firstName = parts.Length > 1 ? string.Join(" ", parts[..^1]) : string.Empty;
            string lastName = parts[^1];
            const string sql = """
                DECLARE @AuthorID INT;
                SELECT TOP 1 @AuthorID = AuthorID FROM dbo.Authors
                WHERE FirstName = @FirstName AND LastName = @LastName;
                IF @AuthorID IS NULL
                BEGIN
                    INSERT INTO dbo.Authors (FirstName, LastName)
                    VALUES (@FirstName, @LastName);
                    SET @AuthorID = CONVERT(INT, SCOPE_IDENTITY());
                END;
                INSERT INTO dbo.BookAuthors (BookID, AuthorID) VALUES (@BookID, @AuthorID);
                """;
            await using SqlCommand command = new(sql, connection, transaction);
            command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = firstName;
            command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = lastName;
            command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await command.ExecuteNonQueryAsync();
        }

        private static async Task AddGenreAsync(SqlConnection connection, SqlTransaction transaction, int bookId, string? genre)
        {
            if (string.IsNullOrWhiteSpace(genre)) return;
            const string sql = """
                DECLARE @GenreID INT;
                SELECT TOP 1 @GenreID = GenreID FROM dbo.Genres WHERE GenreName = @GenreName;
                IF @GenreID IS NULL
                BEGIN
                    INSERT INTO dbo.Genres (GenreName) VALUES (@GenreName);
                    SET @GenreID = CONVERT(INT, SCOPE_IDENTITY());
                END;
                INSERT INTO dbo.BookGenres (BookID, GenreID) VALUES (@BookID, @GenreID);
                """;
            await using SqlCommand command = new(sql, connection, transaction);
            command.Parameters.Add("@GenreName", SqlDbType.NVarChar, 100).Value = genre.Trim();
            command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await command.ExecuteNonQueryAsync();
        }

        private static async Task<List<string>> GetBookAuthorsAsync(SqlConnection connection, int bookId)
        {
            const string sql = """
                SELECT CONCAT(a.FirstName, ' ', a.LastName)
                FROM dbo.Authors a
                INNER JOIN dbo.BookAuthors ba ON ba.AuthorID = a.AuthorID
                WHERE ba.BookID = @BookID
                ORDER BY a.LastName, a.FirstName;
                """;
            await using SqlCommand command = new(sql, connection);
            command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<string> values = new();
            while (await reader.ReadAsync()) values.Add(reader.GetString(0).Trim());
            return values;
        }

        private static async Task<List<string>> GetBookGenresAsync(SqlConnection connection, int bookId)
        {
            const string sql = """
                SELECT g.GenreName
                FROM dbo.Genres g
                INNER JOIN dbo.BookGenres bg ON bg.GenreID = g.GenreID
                WHERE bg.BookID = @BookID
                ORDER BY g.GenreName;
                """;
            await using SqlCommand command = new(sql, connection);
            command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            List<string> values = new();
            while (await reader.ReadAsync()) values.Add(reader.GetString(0));
            return values;
        }

        private static async Task<bool> BookExistsAsync(SqlConnection connection, SqlTransaction transaction, int bookId)
        {
            const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Books WHERE BookID = @BookID) THEN 1 ELSE 0 END;";
            await using SqlCommand command = new(sql, connection, transaction);
            command.Parameters.Add("@BookID", SqlDbType.Int).Value = bookId;
            return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
        }

        private static void AddNullable(SqlCommand command, string name, SqlDbType type, int? size, object? value)
        {
            SqlParameter parameter = size.HasValue
                ? command.Parameters.Add(name, type, size.Value)
                : command.Parameters.Add(name, type);
            parameter.Value = value ?? DBNull.Value;
        }
    }
}
