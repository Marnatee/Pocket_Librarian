using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

public interface IBookDonationRepository
{
    Task<int> SubmitAsync(BookDonationSubmission submission);

    Task<List<BookDonation>> GetPendingAsync();

    Task<bool> ApproveAsync(int donationId, int staffUserId, int? existingBookId, string? reviewNotes);

    Task<bool> RejectAsync(int donationId, int staffUserId, string? reviewNotes);
}
