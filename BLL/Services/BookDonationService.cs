using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public sealed class BookDonationService
{
    private const int MaxCoverImageBytes = 5 * 1024 * 1024;
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
        submission.BookCondition = OptionalText(submission.BookCondition, 100);
        submission.DonationComments = OptionalText(submission.DonationComments, 4000);

        if (submission.UserId <= 0)
        {
            throw new ArgumentException("A signed-in member account is required.");
        }

        if (submission.NumberOfCopies < 1 || submission.NumberOfCopies > 1000)
        {
            throw new ArgumentException("Number of copies must be between 1 and 1,000.");
        }

        if (submission.PublicationYear is < 0 or > 9999)
        {
            throw new ArgumentException("Publication year must be a valid year.");
        }

        if (submission.CoverImage is not null && submission.CoverImage.Length > MaxCoverImageBytes)
        {
            throw new ArgumentException("The cover image cannot exceed 5 MB.");
        }

        return await _repository.SubmitAsync(submission);
    }

    public Task<List<BookDonation>> GetPendingAsync() => _repository.GetPendingAsync();

    public Task<bool> ApproveAsync(int donationId, int staffUserId, BookDonationReview review)
    {
        ValidateReview(donationId, staffUserId, review);
        return _repository.ApproveAsync(
            donationId,
            staffUserId,
            review.ExistingBookId,
            OptionalText(review.ReviewNotes, 4000));
    }

    public Task<bool> RejectAsync(int donationId, int staffUserId, BookDonationReview review)
    {
        ValidateReview(donationId, staffUserId, review);
        return _repository.RejectAsync(
            donationId,
            staffUserId,
            OptionalText(review.ReviewNotes, 4000));
    }

    private static void ValidateReview(int donationId, int staffUserId, BookDonationReview review)
    {
        if (donationId <= 0 || staffUserId <= 0)
        {
            throw new ArgumentException("A valid donation and staff account are required.");
        }

        if (review.ExistingBookId is <= 0)
        {
            review.ExistingBookId = null;
        }
    }

    private static string RequiredText(string? value, string fieldName, int maxLength)
    {
        string text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            throw new ArgumentException($"{fieldName} is required.");
        }

        if (text.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return text;
    }

    private static string? OptionalText(string? value, int maxLength)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (text is not null && text.Length > maxLength)
        {
            throw new ArgumentException($"Text fields cannot exceed {maxLength} characters.");
        }

        return text;
    }
}
