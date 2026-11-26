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

    // Contains the criteria that can be used to search the catalogue.
    // Empty criteria are ignored so users can search using any combination of fields.
    public class CatalogueSearchCriteria
    {
        public string Title { get; set; } = string.Empty;

        public string ISBN { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public int? GenreId { get; set; }

        public bool AvailableOnly { get; set; }
    }

    // Represents one book returned by a catalogue search.
    // Includes book information along with its associated authors and genres.
    public class CatalogueBook
    {
        public int BookId { get; set; }

        public string ISBN { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int? PublicationYear { get; set; }

        public string? CoverImagePath { get; set; }

        public string SourceType { get; set; } = "Library acquisition";

        public string VerificationStatus { get; set; } = "Approved";

        public int TotalCopies { get; set; }

        public int CopiesAvailable { get; set; }

        public List<string> Authors { get; set; } = new List<string>();

        public List<string> Genres { get; set; } = new List<string>();
    }

    // The editable metadata submitted by a staff member for a catalogue record.
    public class CatalogueBookMutation
    {
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? ISBN { get; set; }
        public string? Genre { get; set; }
        public int? PublicationYear { get; set; }
        public string? Description { get; set; }
        public string? CoverImagePath { get; set; }
        public string SourceType { get; set; } = "Library acquisition";
    }
}
