// Data-transfer model
// Carries a temporary demo connection string from React to the local API.

namespace Pocket_Librarian.BLL.Models
{
    public class ManualDatabaseConnectionRequest
    {
        public string ConnectionString { get; set; } = "";
    }
}
