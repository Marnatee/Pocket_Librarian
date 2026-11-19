namespace Pocket_Librarian.BLL.Models
{
    // Represents a genre available for catalogue searching and filtering.
    // Genre data is retrieved from the database by the DAL and passed
    // through the application using this model.
    public class CatalogueGenre
    {
        // Unique identifier for the genre in the database.
        public int GenreId { get; set; }

        // Display name shown to users in the catalogue genre dropdown.
        public string GenreName { get; set; } = string.Empty;
    }
}