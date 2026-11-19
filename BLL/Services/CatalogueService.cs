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
    }
}