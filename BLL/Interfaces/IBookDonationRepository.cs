using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

public interface IBookDonationRepository
{
    Task<int> SubmitAsync(BookDonationSubmission submission);

    Task<List<BookDonation>> GetPendingAsync();

    Task<BookDonation?> GetByIdAsync(int donationId);

    Task<List<BookDonation>> GetByStatusAsync(string status);

    Task<bool> UpdateAsync(BookDonation donation);

    Task<bool> VerifyAsync(int donationId, int staffUserId, string? reviewNotes);

    Task<bool> ApproveAsync(int donationId, int staffUserId, int? existingBookId, string? reviewNotes);

    Task<bool> RejectAsync(int donationId, int staffUserId, string? reviewNotes);

    Task<bool> ArchiveAsync(int donationId, int staffUserId, string? reviewNotes);

    Task<bool> DeleteAsync(int donationId);
}
