using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public sealed class DonationService
{
    private readonly IDonationRepository _repository;

    public DonationService(IDonationRepository repository) => _repository = repository;

    public Task<List<Donation>> GetPendingDonationsAsync() => _repository.GetPendingDonationsAsync();

    public Task<List<Donation>> GetDonationsByStatusAsync(string status)
    {
        ValidateStatus(status);
        return _repository.GetDonationsByStatusAsync(status);
    }

    public Task<Donation?> GetDonationByIdAsync(int donationId)
    {
        ValidateId(donationId, nameof(donationId));
        return _repository.GetDonationByIdAsync(donationId);
    }

    public async Task<bool> UpdateDonationAsync(int donationId, Donation donation)
    {
        ValidateId(donationId, nameof(donationId));
        if (donation is null) throw new ArgumentNullException(nameof(donation));
        donation.DonationId = donationId;
        ValidateDonation(donation);
        var current = await _repository.GetDonationByIdAsync(donationId)
            ?? throw new InvalidOperationException("Donation not found.");
        if (current.VerificationStatus is not ("Pending" or "Verified"))
            throw new InvalidOperationException("Only pending or verified donations can be edited.");
        if (!string.IsNullOrWhiteSpace(donation.ISBN)
            && await _repository.ISBNExistsAsync(donation.ISBN, donationId))
            throw new InvalidOperationException("A book with that ISBN already exists.");
        donation.CoverImagePath ??= current.CoverImagePath;
        return await _repository.UpdateDonationAsync(donation);
    }

    public Task<bool> VerifyDonationAsync(int donationId, int staffId, string? notes)
    {
        ValidateCommand(donationId, staffId);
        return _repository.UpdateDonationStatusAsync(donationId, "Verified", staffId, notes);
    }

    public async Task<bool> ApproveDonationAsync(int donationId, int? existingBookId, int staffId, string? notes)
    {
        ValidateCommand(donationId, staffId);
        if (existingBookId is <= 0) existingBookId = null;
        var donation = await _repository.GetDonationByIdAsync(donationId)
            ?? throw new InvalidOperationException("Donation not found.");
        if (donation.VerificationStatus is "Approved" or "Rejected" or "Archived")
            throw new InvalidOperationException($"Cannot approve a {donation.VerificationStatus} donation.");
        return await _repository.ApproveDonationAsync(donationId, existingBookId, staffId, notes);
    }

    public Task<bool> RejectDonationAsync(int donationId, int staffId, string? reason)
    {
        ValidateCommand(donationId, staffId);
        return _repository.RejectDonationAsync(donationId, staffId, reason);
    }

    public Task<bool> ArchiveDonationAsync(int donationId, int staffId, string? notes)
    {
        ValidateCommand(donationId, staffId);
        return _repository.ArchiveDonationAsync(donationId, staffId, notes);
    }

    public Task<bool> DeleteDonationAsync(int donationId)
    {
        ValidateId(donationId, nameof(donationId));
        return _repository.DeleteDonationAsync(donationId);
    }

    private static void ValidateDonation(Donation donation)
    {
        if (string.IsNullOrWhiteSpace(donation.Title)) throw new ArgumentException("Title is required.");
        if (donation.Title.Length > 255) throw new ArgumentException("Title cannot exceed 255 characters.");
        if (donation.Author?.Length > 255) throw new ArgumentException("Author cannot exceed 255 characters.");
        if (donation.ISBN?.Length > 20) throw new ArgumentException("ISBN cannot exceed 20 characters.");
        if (donation.Genre?.Length > 100) throw new ArgumentException("Genre cannot exceed 100 characters.");
        if (donation.Description?.Length > 4000) throw new ArgumentException("Description cannot exceed 4,000 characters.");
        if (donation.CopiesDonated is < 1 or > 1000) throw new ArgumentException("Copies must be between 1 and 1,000.");
        if (donation.PublicationYear is < 0 or > 9999) throw new ArgumentException("Publication year is invalid.");
        if (string.IsNullOrWhiteSpace(donation.SourceType)) throw new ArgumentException("Source type is required.");
        if (donation.Condition is not ("Like New" or "Good" or "Fair" or "Poor"))
            throw new ArgumentException("Invalid copy condition.");
    }

    private static void ValidateStatus(string status)
    {
        if (status is not ("Pending" or "Verified" or "Approved" or "Rejected" or "Archived"))
            throw new ArgumentException("Invalid donation status.", nameof(status));
    }

    private static void ValidateId(int id, string name)
    {
        if (id <= 0) throw new ArgumentException($"{name} must be greater than 0.", name);
    }

    private static void ValidateCommand(int donationId, int staffId)
    {
        ValidateId(donationId, nameof(donationId));
        ValidateId(staffId, nameof(staffId));
    }
}
