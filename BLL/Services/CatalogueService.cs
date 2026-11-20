using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services
{
    // Handles business logic for catalogue-related operations.
    // Data retrieval is delegated to the catalogue repository.
    public class CatalogueService
    {
        private readonly ICatalogueRepository _repository;

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
        public List<CatalogueBook> SearchBooks(CatalogueSearchCriteria criteria)
        {
            criteria.Title = criteria.Title.Trim();
            criteria.ISBN = criteria.ISBN.Trim();
            criteria.Author = criteria.Author.Trim();

            return _repository.SearchBooks(criteria);
        }
    }
}