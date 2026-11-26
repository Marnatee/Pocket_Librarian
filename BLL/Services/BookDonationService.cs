using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public sealed class BookDonationService
{
    private readonly IBookDonationRepository _repository;

    public BookDonationService(IBookDonationRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> SubmitAsync(BookDonationSubmission submission)
    {
        submission.Title = RequiredText(submission.Title, "Title", 255);
        submission.Author = OptionalText(submission.Author, 255);
        submission.ISBN = OptionalText(submission.ISBN, 20);
        submission.Genre = OptionalText(submission.Genre, 100);
        submission.Description = OptionalText(submission.Description, 4000);
        submission.BookCondition = OptionalText(submission.BookCondition, 30) ?? "Good";
        submission.DonationComments = OptionalText(submission.DonationComments, 4000);
        submission.SourceType = RequiredText(submission.SourceType, "Source type", 100);

        if (submission.UserId <= 0)
            throw new ArgumentException("A signed-in member account is required.");
        if (submission.NumberOfCopies is < 1 or > 1000)
            throw new ArgumentException("Number of copies must be between 1 and 1,000.");
        if (submission.PublicationYear is < 0 or > 9999)
            throw new ArgumentException("Publication year must be a valid year.");

        return await _repository.SubmitAsync(submission);
    }

    public Task<List<BookDonation>> GetPendingAsync() => _repository.GetPendingAsync();

    public Task<BookDonation?> GetByIdAsync(int donationId)
    {
        ValidateId(donationId, nameof(donationId));
        return _repository.GetByIdAsync(donationId);
    }

    public Task<List<BookDonation>> GetByStatusAsync(string status)
    {
        ValidateStatus(status);
        return _repository.GetByStatusAsync(status);
    }

    public async Task<bool> UpdateAsync(
        int donationId,
        string title,
        string? author,
        string? isbn,
        string? genre,
        int? publicationYear,
        string? description,
        string? condition,
        int copies,
        string sourceType,
        string? coverImagePath)
    {
        ValidateId(donationId, nameof(donationId));
        var donation = await _repository.GetByIdAsync(donationId)
            ?? throw new InvalidOperationException("Donation not found.");

        if (donation.VerificationStatus is not ("Pending" or "Verified"))
            throw new InvalidOperationException("Only pending or verified donations can be edited.");

        donation.Title = RequiredText(title, "Title", 255);
        donation.Author = OptionalText(author, 255);
        donation.ISBN = OptionalText(isbn, 20);
        donation.Genre = OptionalText(genre, 100);
        donation.Description = OptionalText(description, 4000);
        donation.BookCondition = OptionalText(condition, 30) ?? "Good";
        donation.NumberOfCopies = ValidateCopies(copies);
        donation.SourceType = RequiredText(sourceType, "Source type", 100);
        donation.CoverImagePath = string.IsNullOrWhiteSpace(coverImagePath)
            ? donation.CoverImagePath
            : coverImagePath;
        donation.PublicationYear = publicationYear;

        return await _repository.UpdateAsync(donation);

        return await _repository.UpdateAsync(donation);
    }

    public Task<bool> VerifyAsync(int donationId, int staffId, string? notes)
    {
        ValidateCommand(donationId, staffId);
        return _repository.VerifyAsync(donationId, staffId, notes);
    }

    public async Task<bool> ApproveAsync(int donationId, int staffId, int? existingBookId, string? notes)
    {
        ValidateCommand(donationId, staffId);
        if (existingBookId is <= 0) existingBookId = null;

        var donation = await _repository.GetByIdAsync(donationId)
            ?? throw new InvalidOperationException("Donation not found.");
        if (donation.VerificationStatus is "Rejected" or "Archived" or "Approved")
            throw new InvalidOperationException($"Cannot approve a {donation.VerificationStatus} donation.");

        return await _repository.ApproveAsync(donationId, staffId, existingBookId, notes);
    }

    public Task<bool> RejectAsync(int donationId, int staffId, string? reason)
    {
        ValidateCommand(donationId, staffId);
        return _repository.RejectAsync(donationId, staffId, reason);
    }

    public Task<bool> ArchiveAsync(int donationId, int staffId, string? notes)
    {
        ValidateCommand(donationId, staffId);
        return _repository.ArchiveAsync(donationId, staffId, notes);
    }

    public Task<bool> DeleteAsync(int donationId)
    {
        ValidateId(donationId, nameof(donationId));
        return _repository.DeleteAsync(donationId);
    }

    private static int ValidateCopies(int copies)
    {
        if (copies is < 1 or > 1000)
            throw new ArgumentException("Number of copies must be between 1 and 1,000.");
        return copies;
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

    private static void ValidateStatus(string status)
    {
        if (status is not ("Pending" or "Verified" or "Approved" or "Rejected" or "Archived"))
            throw new ArgumentException("Invalid donation status.", nameof(status));
    }

    private static string RequiredText(string? value, string name, int maxLength)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0) throw new ArgumentException($"{name} is required.");
        if (text.Length > maxLength) throw new ArgumentException($"{name} cannot exceed {maxLength} characters.");
        return text;
    }

    private static string? OptionalText(string? value, int maxLength)
    {
        var text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (text is not null && text.Length > maxLength)
            throw new ArgumentException("Text fields cannot exceed the permitted length.");
        return text;
    }
}
