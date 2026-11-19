using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public sealed class MemberProfileService
{
    private readonly IMemberProfileRepository _repository;

    public MemberProfileService(IMemberProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<MemberProfile?> GetByUserIdAsync(int userId)
    {
        ValidateUserId(userId);
        return _repository.GetByUserIdAsync(userId);
    }

    public async Task<(bool Succeeded, string Message, MemberProfile? Profile)>
        UpdateAsync(
            int userId,
            UpdateMemberProfileRequest request)
    {
        ValidateUserId(userId);

        string firstName = request.FirstName.Trim();
        string lastName = request.LastName.Trim();
        string email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(firstName)
            || string.IsNullOrWhiteSpace(lastName)
            || string.IsNullOrWhiteSpace(email))
        {
            return (false, "First name, last name, and email are required.", null);
        }

        if (!email.Contains('@'))
        {
            return (false, "A valid email address is required.", null);
        }

        if (await _repository.EmailExistsForOtherUserAsync(email, userId))
        {
            return (false, "That email address is already in use.", null);
        }

        bool updated = await _repository.UpdateAsync(
            userId,
            new UpdateMemberProfileRequest
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email
            });

        if (!updated)
        {
            return (false, "The member profile was not found.", null);
        }

        MemberProfile? profile = await _repository.GetByUserIdAsync(userId);
        return (true, "Member profile updated successfully.", profile);
    }

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "User ID must be greater than zero.",
                nameof(userId));
        }
    }
}
