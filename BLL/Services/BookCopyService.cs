// Business Logic Layer.
// Validates book copy commands and delegates persistence to the DAL.

using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public class BookCopyService
{
    private readonly IBookCopyRepository _repository;

    public BookCopyService(IBookCopyRepository repository)
    {
        _repository = repository;
    }

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

    public Task<List<BookCopy>> GetAvailableCopiesByBookIdAsync(int bookId)
    {
        ValidateId(bookId, nameof(bookId));
        return _repository.GetAvailableCopiesByBookIdAsync(bookId);
    }

    public async Task<BookCopy?> GetCopyByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.", nameof(barcode));

        return await _repository.GetCopyByBarcodeAsync(barcode);
    }

    public async Task<int> CreateCopyAsync(
        int bookId,
        string? barcode,
        string condition,
        string location)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateCondition(condition);

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var copy = new BookCopy
        {
            BookId = bookId,
            Barcode = barcode?.Trim(),
            Condition = condition,
            Status = "Available",
            Location = location.Trim(),
            DateAcquired = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        return await _repository.CreateCopyAsync(copy);
    }

    public async Task<List<int>> CreateCopiesAsync(
        int bookId,
        int count,
        string condition,
        string location)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateCondition(condition);

        if (count <= 0 || count > 100)
            throw new ArgumentException("Copy count must be between 1 and 100.", nameof(count));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var copies = new List<BookCopy>();
        for (int i = 0; i < count; i++)
        {
            copies.Add(new BookCopy
            {
                BookId = bookId,
                Condition = condition,
                Status = "Available",
                Location = location.Trim(),
                DateAcquired = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            });
        }

        return await _repository.CreateCopiesAsync(copies);
    }

    public async Task<bool> UpdateCopyAsync(int copyId, string condition, string location, string? notes = null)
    {
        ValidateId(copyId, nameof(copyId));
        ValidateCondition(condition);

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var copy = await _repository.GetCopyByIdAsync(copyId);
        if (copy is null)
            throw new InvalidOperationException($"Copy with ID {copyId} not found.");

        copy.Condition = condition;
        copy.Location = location.Trim();
        if (!string.IsNullOrWhiteSpace(notes))
            copy.Notes = notes;
        copy.DateModified = DateTime.UtcNow;

        return await _repository.UpdateCopyAsync(copy);
    }

    public async Task<bool> UpdateCopyStatusAsync(int copyId, string newStatus)
    {
        ValidateId(copyId, nameof(copyId));
        ValidateStatus(newStatus);

        return await _repository.UpdateCopyStatusAsync(copyId, newStatus);
    }

    public async Task<bool> UpdateCopyConditionAsync(int copyId, string newCondition)
    {
        ValidateId(copyId, nameof(copyId));
        ValidateCondition(newCondition);

        return await _repository.UpdateCopyConditionAsync(copyId, newCondition);
    }

    public async Task<bool> UpdateCopyLocationAsync(int copyId, string location)
    {
        ValidateId(copyId, nameof(copyId));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        return await _repository.UpdateCopyLocationAsync(copyId, location);
    }

    public async Task<bool> CheckOutCopyAsync(int copyId, int reservationId, DateTime dueDate)
    {
        ValidateId(copyId, nameof(copyId));
        ValidateId(reservationId, nameof(reservationId));

        if (dueDate <= DateTime.UtcNow)
            throw new ArgumentException("Due date must be in the future.", nameof(dueDate));

        return await _repository.CheckOutCopyAsync(copyId, reservationId, dueDate);
    }

    public async Task<bool> ReturnCopyAsync(int copyId)
    {
        ValidateId(copyId, nameof(copyId));

        return await _repository.ReturnCopyAsync(copyId);
    }

    public async Task<bool> DeleteCopyAsync(int copyId)
    {
        ValidateId(copyId, nameof(copyId));

        var copy = await _repository.GetCopyByIdAsync(copyId);
        if (copy is null)
            throw new InvalidOperationException($"Copy with ID {copyId} not found.");

        return await _repository.DeleteCopyAsync(copyId);
    }

    public Task<int> GetCopyCountByStatusAsync(int bookId, string status)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateStatus(status);

        return _repository.GetCopyCountByStatusAsync(bookId, status);
    }

    public Task<List<BookCopy>> GetCopiesNeedingAttentionAsync() =>
        _repository.GetCopiesNeedingAttentionAsync();

    //Validation Helpers

    private static void ValidateId(int id, string paramName)
    {
        if (id <= 0)
            throw new ArgumentException($"{paramName} must be greater than 0", paramName);
    }

    private static void ValidateCondition(string condition)
    {
        var validConditions = new[] { "Like New", "Good", "Fair", "Poor" };
        if (!validConditions.Contains(condition))
            throw new ArgumentException("Invalid condition value.", nameof(condition));
    }

    private static void ValidateStatus(string status)
    {
        var validStatuses = new[] { "Available", "Checked Out", "Damaged", "Lost", "Removed" };
        if (!validStatuses.Contains(status))
            throw new ArgumentException("Invalid status value.", nameof(status));
    }
}
