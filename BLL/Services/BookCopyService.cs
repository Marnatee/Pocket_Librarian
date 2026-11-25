using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public sealed class BookCopyService
{
    private static readonly string[] Conditions = ["Like New", "Good", "Fair", "Poor"];
    private static readonly string[] Statuses = ["Available", "Checked Out", "Damaged", "Lost", "Removed"];
    private readonly IBookCopyRepository _repository;

    public BookCopyService(IBookCopyRepository repository) => _repository = repository;

    public Task<List<BookCopy>> GetCopiesByBookIdAsync(int bookId)
    {
        ValidateId(bookId, nameof(bookId));
        return _repository.GetCopiesByBookIdAsync(bookId);
    }

    public Task<BookCopy?> GetCopyByIdAsync(int copyId)
    {
        ValidateId(copyId, nameof(copyId));
        return _repository.GetCopyByIdAsync(copyId);
    }

    public async Task<int> CreateCopyAsync(int bookId, string? barcode, string condition, string? location)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateCondition(condition);
        var copy = new BookCopy
        {
            BookId = bookId,
            Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim(),
            Condition = condition,
            Status = "Available",
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim()
        };
        return await _repository.CreateCopyAsync(copy);
    }

    public async Task<List<int>> CreateCopiesAsync(int bookId, int count, string condition, string? location)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateCondition(condition);
        if (count is < 1 or > 1000)
            throw new ArgumentException("Copy count must be between 1 and 1,000.", nameof(count));
        return await _repository.CreateCopiesAsync(bookId, count, condition, location);
    }

    public async Task<bool> UpdateCopyAsync(int copyId, string condition, string? location, string? notes, string? status)
    {
        ValidateId(copyId, nameof(copyId));
        ValidateCondition(condition);
        if (!string.IsNullOrWhiteSpace(status)) ValidateStatus(status);
        var copy = await _repository.GetCopyByIdAsync(copyId)
            ?? throw new InvalidOperationException("Copy not found.");
        copy.Condition = condition;
        copy.Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        copy.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (!string.IsNullOrWhiteSpace(status)) copy.Status = status;
        copy.DateModified = DateTime.UtcNow;
        return await _repository.UpdateCopyAsync(copy);
    }

    public Task<bool> DeleteCopyAsync(int copyId)
    {
        ValidateId(copyId, nameof(copyId));
        return _repository.DeleteCopyAsync(copyId);
    }

    private static void ValidateId(int id, string name)
    {
        if (id <= 0) throw new ArgumentException($"{name} must be greater than 0.", name);
    }

    private static void ValidateCondition(string condition)
    {
        if (!Conditions.Contains(condition))
            throw new ArgumentException("Invalid copy condition.", nameof(condition));
    }

    private static void ValidateStatus(string status)
    {
        if (!Statuses.Contains(status))
            throw new ArgumentException("Invalid copy status.", nameof(status));
    }
}
