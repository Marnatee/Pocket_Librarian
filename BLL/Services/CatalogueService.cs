using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services
{
    // Handles business logic for catalogue-related operations.
    // Data retrieval is delegated to the catalogue repository.
    public class CatalogueService
    {
        private readonly ICatalogueRepository _repository;
        // Search limits mirror the sizes of the corresponding database fields.
        private const int MaxTitleLength = 255;
        private const int MaxIsbnLength = 13;
        private const int MaxAuthorLength = 101;

        // Receives the catalogue repository used by the service for data access.
        public CatalogueService(ICatalogueRepository repository)
        {
            _repository = repository;
        }

        // Retrieves the genres available for catalogue filtering.
        public List<CatalogueGenre> GetGenres()
        {
            return _repository.GetGenres();
        }

        // Searches the catalogue using the provided criteria.
        // Search text is trimmed before being passed to the data-access layer.
        // Validates and normalizes catalogue search criteria before data access.
        public List<CatalogueBook> SearchBooks(CatalogueSearchCriteria criteria)
        {
            criteria.Title = (criteria.Title ?? string.Empty).Trim();
            criteria.ISBN = (criteria.ISBN ?? string.Empty).Trim();
            criteria.Author = (criteria.Author ?? string.Empty).Trim();

            if (criteria.Title.Length > MaxTitleLength)
            {
                throw new ArgumentException(
                    $"Title search cannot exceed {MaxTitleLength} characters.");
            }

            if (criteria.ISBN.Length > MaxIsbnLength)
            {
                throw new ArgumentException(
                    $"ISBN search cannot exceed {MaxIsbnLength} characters.");
            }

            if (criteria.Author.Length > MaxAuthorLength)
            {
                throw new ArgumentException(
                    $"Author search cannot exceed {MaxAuthorLength} characters.");
            }

            if (criteria.GenreId.HasValue && criteria.GenreId.Value <= 0)
            {
                throw new ArgumentException(
                    "Genre ID must be greater than zero.");
            }

            return _repository.SearchBooks(criteria);
        }
    }
}