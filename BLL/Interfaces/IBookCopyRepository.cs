using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

public interface IBookCopyRepository
{
    Task<List<BookCopy>> GetCopiesByBookIdAsync(int bookId);
    Task<BookCopy?> GetCopyByIdAsync(int copyId);
    Task<List<BookCopy>> GetAvailableCopiesByBookIdAsync(int bookId);
    Task<BookCopy?> GetCopyByBarcodeAsync(string barcode);
    Task<int> CreateCopyAsync(BookCopy copy);
    Task<List<int>> CreateCopiesAsync(List<BookCopy> copies);
    Task<List<int>> CreateCopiesAsync(int bookId, int count, string condition, string? location);
    Task<bool> UpdateCopyAsync(BookCopy copy);
    Task<bool> UpdateCopyStatusAsync(int copyId, string newStatus);
    Task<bool> UpdateCopyConditionAsync(int copyId, string newCondition);
    Task<bool> UpdateCopyLocationAsync(int copyId, string? location);
    Task<bool> CheckOutCopyAsync(int copyId, int reservationId, DateTime dueDate);
    Task<bool> ReturnCopyAsync(int copyId);
    Task<bool> DeleteCopyAsync(int copyId);
    Task<int> GetCopyCountByStatusAsync(int bookId, string status);
    Task<List<BookCopy>> GetCopiesNeedingAttentionAsync();
}
