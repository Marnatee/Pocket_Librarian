using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

public interface IMemberProfileRepository
{
    Task<MemberProfile?> GetByUserIdAsync(int userId);

    Task<bool> EmailExistsForOtherUserAsync(string email, int userId);

    Task<bool> UpdateAsync(
        int userId,
        UpdateMemberProfileRequest request);
}
