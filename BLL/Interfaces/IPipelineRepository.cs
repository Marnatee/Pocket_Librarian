namespace Pocket_Librarian.BLL.Interfaces;

public interface IPipelineRepository
{
    (bool Connected, string Message, string Details) TestDataAccess();

    (bool Connected, string Message, string Details) TestManualDataAccess(
        string connectionString);
}
