using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.DAL.Repositories;

// Compatibility adapter for the original member donation endpoints.
// The administration workflow uses the same DonationRepository and tables.
public sealed class BookDonationRepository : IBookDonationRepository
{
    private readonly IDonationRepository _repository;

    public BookDonationRepository(IDonationRepository repository) => _repository = repository;

    public async Task<int> SubmitAsync(BookDonationSubmission submission)
    {
        return await _repository.CreateDonationAsync(new Donation
        {
            SubmittedByUserId = submission.UserId,
            Title = submission.Title,
            Author = submission.Author,
            ISBN = submission.ISBN,
            Genre = submission.Genre,
            PublicationYear = submission.PublicationYear,
            Description = submission.Description,
            Condition = submission.BookCondition ?? "Good",
            CopiesDonated = submission.NumberOfCopies,
            CoverImagePath = submission.CoverImagePath,
            SourceType = submission.SourceType,
            DonationComments = submission.DonationComments
        });
    }

    public async Task<List<BookDonation>> GetPendingAsync() =>
        (await _repository.GetPendingDonationsAsync()).Select(ToBookDonation).ToList();

    public async Task<BookDonation?> GetByIdAsync(int donationId)
    {
        var donation = await _repository.GetDonationByIdAsync(donationId);
        return donation is null ? null : ToBookDonation(donation);
    }

    public async Task<List<BookDonation>> GetByStatusAsync(string status) =>
        (await _repository.GetDonationsByStatusAsync(NormalizeStatus(status)))
            .Select(ToBookDonation)
            .ToList();

    public async Task<bool> UpdateAsync(BookDonation donation)
    {
        var current = await _repository.GetDonationByIdAsync(donation.DonationId);
        if (current is null) return false;
        current.Title = donation.Title;
        current.Author = donation.Author;
        current.ISBN = donation.ISBN;
        current.Genre = donation.Genre;
        current.PublicationYear = donation.PublicationYear;
        current.Description = donation.Description;
        current.Condition = donation.BookCondition ?? "Good";
        current.CopiesDonated = donation.NumberOfCopies;
        current.SourceType = donation.SourceType;
        current.CoverImagePath = donation.CoverImagePath;
        current.DonationComments = donation.DonationComments;
        return await _repository.UpdateDonationAsync(current);
    }

    public Task<bool> VerifyAsync(int donationId, int staffUserId, string? reviewNotes) =>
        _repository.UpdateDonationStatusAsync(donationId, "Verified", staffUserId, reviewNotes);

    public Task<bool> ApproveAsync(int donationId, int staffUserId, int? existingBookId, string? reviewNotes) =>
        _repository.ApproveDonationAsync(donationId, existingBookId, staffUserId, reviewNotes);

    public Task<bool> RejectAsync(int donationId, int staffUserId, string? reviewNotes) =>
        _repository.RejectDonationAsync(donationId, staffUserId, reviewNotes);

    public Task<bool> ArchiveAsync(int donationId, int staffUserId, string? reviewNotes) =>
        _repository.ArchiveDonationAsync(donationId, staffUserId, reviewNotes);

    public Task<bool> DeleteAsync(int donationId) => _repository.DeleteDonationAsync(donationId);

    private static string NormalizeStatus(string status) =>
        status == "Pending Verification" ? "Pending" : status;

    private static BookDonation ToBookDonation(Donation donation) =>
        new()
        {
            DonationId = donation.DonationId,
            UserId = donation.SubmittedByUserId ?? 0,
            DonorName = donation.DonorName,
            DonorEmail = donation.DonorEmail,
            Title = donation.Title,
            Author = donation.Author,
            ISBN = donation.ISBN,
            Genre = donation.Genre,
            PublicationYear = donation.PublicationYear,
            Description = donation.Description,
            BookCondition = donation.Condition,
            NumberOfCopies = donation.CopiesDonated,
            HasCoverImage = !string.IsNullOrWhiteSpace(donation.CoverImagePath),
            CoverImagePath = donation.CoverImagePath,
            SourceType = donation.SourceType,
            DonationComments = donation.DonationComments,
            Status = donation.VerificationStatus == "Pending" ? "Pending Verification" : donation.VerificationStatus,
            VerificationStatus = donation.VerificationStatus,
            ApprovedBookId = donation.BookId,
            ReviewNotes = donation.StaffNotes,
            SubmittedAt = donation.DateSubmitted
        };
}
