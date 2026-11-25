using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

public interface IDonationRepository
{
    Task<List<Donation>> GetPendingDonationsAsync();
    Task<List<Donation>> GetDonationsByStatusAsync(string status);
    Task<Donation?> GetDonationByIdAsync(int donationId);
    Task<int> CreateDonationAsync(Donation donation);
    Task<bool> UpdateDonationAsync(Donation donation);
    Task<bool> UpdateDonationStatusAsync(int donationId, string newStatus, int staffId, string? notes = null);
    Task<bool> ApproveDonationAsync(int donationId, int? existingBookId, int staffId, string? notes = null);
    Task<bool> RejectDonationAsync(int donationId, int staffId, string? reason = null);
    Task<bool> ArchiveDonationAsync(int donationId, int staffId, string? notes = null);
    Task<bool> DeleteDonationAsync(int donationId);
    Task<bool> ISBNExistsAsync(string isbn, int? excludeDonationId = null);
    Task<List<Donation>> GetApprovedDonationsAsync();
}
