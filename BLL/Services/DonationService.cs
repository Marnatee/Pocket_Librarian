// Business Logic Layer.
// Validates donation commands and delegates persistence to the DAL.

using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public class DonationService
{
    private readonly IDonationRepository _repository;

    public DonationService(IDonationRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Donation>> GetPendingDonationsAsync() =>
        _repository.GetPendingDonationsAsync();

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

    public async Task<int> CreateDonationAsync(
        string title,
        string author,
        string isbn,
        string genre,
        int? publicationYear,
        string description,
        string sourceType,
        int copiesDonated,
        string condition,
        int? submittedByUserId = null)
    {
        ValidateTitle(title);
        ValidateAuthor(author);
        ValidateISBN(isbn);
        ValidateSourceType(sourceType);
        ValidateCondition(condition);
        ValidateCopiesCount(copiesDonated);

        if (await _repository.ISBNExistsAsync(isbn))
        {
            throw new InvalidOperationException($"A book with ISBN '{isbn}' already exists in the catalogue or pending donations.");
        }

        var donation = new Donation
        {
            Title = title.Trim(),
            Author = author.Trim(),
            ISBN = isbn.Trim(),
            Genre = genre.Trim(),
            PublicationYear = publicationYear,
            Description = description.Trim(),
            SourceType = sourceType,
            CopiesDonated = copiesDonated,
            Condition = condition,
            VerificationStatus = "Pending",
            SubmittedByUserId = submittedByUserId,
            DateSubmitted = DateTime.UtcNow
        };

        return await _repository.CreateDonationAsync(donation);
    }

    public async Task<bool> UpdateDonationAsync(
        int donationId,
        string title,
        string author,
        string isbn,
        string genre,
        int? publicationYear,
        string description,
        string condition)
    {
        ValidateId(donationId, nameof(donationId));
        ValidateTitle(title);
        ValidateAuthor(author);
        ValidateISBN(isbn);
        ValidateCondition(condition);

        var existing = await _repository.GetDonationByIdAsync(donationId);
        if (existing is null)
        {
            throw new InvalidOperationException($"Donation with ID {donationId} not found.");
        }

        if (existing.VerificationStatus != "Pending")
        {
            throw new InvalidOperationException("Only pending donations can be edited.");
        }

        if (existing.ISBN != isbn && await _repository.ISBNExistsAsync(isbn, donationId))
        {
            throw new InvalidOperationException($"A book with ISBN '{isbn}' already exists.");
        }

        existing.Title = title.Trim();
        existing.Author = author.Trim();
        existing.ISBN = isbn.Trim();
        existing.Genre = genre.Trim();
        existing.PublicationYear = publicationYear;
        existing.Description = description.Trim();
        existing.Condition = condition;

        return await _repository.UpdateDonationAsync(existing);
    }

    public Task<bool> VerifyDonationAsync(int donationId, int staffId, string? notes = null)
    {
        ValidateCommand(donationId, staffId);
        return _repository.UpdateDonationStatusAsync(donationId, "Verified", staffId, notes);
    }

    public async Task<bool> ApproveDonationAsync(int donationId, int createdBookId, int staffId)
    {
        ValidateCommand(donationId, staffId);
        ValidateId(createdBookId, nameof(createdBookId));

        var donation = await _repository.GetDonationByIdAsync(donationId);
        if (donation is null)
        {
            throw new InvalidOperationException($"Donation with ID {donationId} not found.");
        }

        if (donation.VerificationStatus == "Rejected" || donation.VerificationStatus == "Archived")
        {
            throw new InvalidOperationException($"Cannot approve a {donation.VerificationStatus} donation.");
        }

        return await _repository.ApproveDonationAsync(donationId, createdBookId, staffId);
    }

    public async Task<bool> RejectDonationAsync(int donationId, int staffId, string? reason = null)
    {
        ValidateCommand(donationId, staffId);

        var donation = await _repository.GetDonationByIdAsync(donationId);
        if (donation is null)
        {
            throw new InvalidOperationException($"Donation with ID {donationId} not found.");
        }

        return await _repository.RejectDonationAsync(donationId, staffId, reason);
    }

    public async Task<bool> ArchiveDonationAsync(int donationId)
    {
        ValidateId(donationId, nameof(donationId));

        var donation = await _repository.GetDonationByIdAsync(donationId);
        if (donation is null)
        {
            throw new InvalidOperationException($"Donation with ID {donationId} not found.");
        }

        return await _repository.ArchiveDonationAsync(donationId);
    }

    public async Task<bool> DeleteDonationAsync(int donationId)
    {
        ValidateId(donationId, nameof(donationId));

        var donation = await _repository.GetDonationByIdAsync(donationId);
        if (donation is null)
        {
            throw new InvalidOperationException($"Donation with ID {donationId} not found.");
        }

        return await _repository.DeleteDonationAsync(donationId);
    }

    public Task<List<Donation>> GetApprovedDonationsAsync() =>
        _repository.GetApprovedDonationsAsync();

    //Validation Helpers

    private static void ValidateId(int id, string paramName)
    {
        if (id <= 0)
            throw new ArgumentException($"{paramName} must be greater than 0", paramName);
    }

    private static void ValidateCommand(int donationId, int staffId)
    {
        ValidateId(donationId, nameof(donationId));
        ValidateId(staffId, nameof(staffId));
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Book title is required.", nameof(title));
    }

    private static void ValidateAuthor(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author name is required.", nameof(author));
    }

    private static void ValidateISBN(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN is required.", nameof(isbn));
    }

    private static void ValidateSourceType(string sourceType)
    {
        var validTypes = new[] { "Publisher donation", "Member donation", "Library acquisition", "Inheritance", "Other" };
        if (!validTypes.Contains(sourceType))
            throw new ArgumentException("Invalid source type.", nameof(sourceType));
    }

    private static void ValidateStatus(string status)
    {
        var validStatuses = new[] { "Pending", "Verified", "Rejected", "Archived" };
        if (!validStatuses.Contains(status))
            throw new ArgumentException("Invalid verification status.", nameof(status));
    }

    private static void ValidateCondition(string condition)
    {
        var validConditions = new[] { "Like New", "Good", "Fair", "Poor" };
        if (!validConditions.Contains(condition))
            throw new ArgumentException("Invalid condition value.", nameof(condition));
    }

    private static void ValidateCopiesCount(int copiesDonated)
    {
        if (copiesDonated <= 0)
            throw new ArgumentException("Number of copies must be greater than 0.", nameof(copiesDonated));
        if (copiesDonated > 100)
            throw new ArgumentException("Cannot donate more than 100 copies at once.", nameof(copiesDonated));
    }
}
