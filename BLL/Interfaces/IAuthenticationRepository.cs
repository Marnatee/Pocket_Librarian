using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

public interface IAuthenticationRepository
{
    bool EmailExists(string email);

    int CreateUser(
        string firstName,
        string lastName,
        string email,
        string passwordHash);

    StoredUser? FindByEmail(string email);
}
