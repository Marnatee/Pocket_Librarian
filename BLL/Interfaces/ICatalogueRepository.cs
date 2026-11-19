using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces
{
    // Defines the data-access operations required by the catalogue feature.
    // The DAL provides the implementation of these operations.
    public interface ICatalogueRepository
    {
        // Retrieves all genres available for catalogue filtering.
        List<CatalogueGenre> GetGenres();

        // Searches the catalogue using the provided search criteria.
        List<CatalogueBook> SearchBooks(CatalogueSearchCriteria criteria);
    }
}