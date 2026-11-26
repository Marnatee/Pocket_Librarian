using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public sealed class CatalogueAdminService
{
    private readonly ICatalogueRepository _catalogueRepository;
    private readonly BookCopyService _bookCopyService;

    public CatalogueAdminService(ICatalogueRepository catalogueRepository, BookCopyService bookCopyService)
    {
        _catalogueRepository = catalogueRepository;
        _bookCopyService = bookCopyService;
    }

    public Task<List<CatalogueBook>> GetBooksAsync() => _catalogueRepository.GetAllBooksAsync();

    public Task<CatalogueBook?> GetBookAsync(int bookId)
    {
        ValidateId(bookId, nameof(bookId));
        return _catalogueRepository.GetBookByIdAsync(bookId);
    }

    public Task<int> CreateBookAsync(CatalogueBookMutation book)
    {
        ValidateBook(book);
        return _catalogueRepository.CreateBookAsync(book);
    }

    public Task<bool> UpdateBookAsync(int bookId, CatalogueBookMutation book)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateBook(book);
        return _catalogueRepository.UpdateBookAsync(bookId, book);
    }

    public Task<bool> DeleteBookAsync(int bookId)
    {
        ValidateId(bookId, nameof(bookId));
        return _catalogueRepository.DeleteBookAsync(bookId);
    }

    public Task<List<BookCopy>> GetCopiesAsync(int bookId) =>
        _bookCopyService.GetCopiesByBookIdAsync(bookId);

    public Task<int> AddCopyAsync(int bookId, string? barcode, string condition, string? location) =>
        _bookCopyService.CreateCopyAsync(bookId, barcode, condition, location);

    public Task<List<int>> AddCopiesAsync(int bookId, int count, string condition, string? location) =>
        _bookCopyService.CreateCopiesAsync(bookId, count, condition, location);

    public Task<bool> UpdateCopyAsync(int copyId, string condition, string? location, string? notes, string? status) =>
        _bookCopyService.UpdateCopyAsync(copyId, condition, location, notes, status);

    public Task<bool> DeleteCopyAsync(int copyId) => _bookCopyService.DeleteCopyAsync(copyId);

    private static void ValidateId(int id, string name)
    {
        if (id <= 0) throw new ArgumentException($"{name} must be greater than 0.", name);
    }

    private static void ValidateBook(CatalogueBookMutation book)
    {
        if (book is null) throw new ArgumentNullException(nameof(book));
        if (string.IsNullOrWhiteSpace(book.Title)) throw new ArgumentException("Title is required.");
        if (book.Title.Trim().Length > 255) throw new ArgumentException("Title cannot exceed 255 characters.");
        if (book.ISBN?.Length > 20) throw new ArgumentException("ISBN cannot exceed 20 characters.");
        if (book.Author?.Length > 255) throw new ArgumentException("Author cannot exceed 255 characters.");
        if (book.Genre?.Length > 100) throw new ArgumentException("Genre cannot exceed 100 characters.");
        if (book.PublicationYear is < 0 or > 9999) throw new ArgumentException("Publication year is invalid.");
        if (string.IsNullOrWhiteSpace(book.SourceType)) throw new ArgumentException("Source type is required.");
    }
}
