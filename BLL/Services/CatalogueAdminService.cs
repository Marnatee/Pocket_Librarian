// Business Logic Layer.
// Validates catalogue administration commands and delegates persistence to the DAL.
// Handles image uploads to the file system.

using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public class CatalogueAdminService
{
    private readonly ICatalogueRepository _catalogueRepository;
    private readonly IBookCopyRepository _bookCopyRepository;
    private readonly string _imageUploadPath;

    public CatalogueAdminService(
        ICatalogueRepository catalogueRepository,
        IBookCopyRepository bookCopyRepository,
        string imageUploadPath = "/images/covers/")
    {
        _catalogueRepository = catalogueRepository;
        _bookCopyRepository = bookCopyRepository;
        _imageUploadPath = imageUploadPath;
    }

    public List<CatalogueBook> SearchBooks(CatalogueSearchCriteria criteria)
    {
        if (criteria == null)
            throw new ArgumentNullException(nameof(criteria));

        return _catalogueRepository.SearchBooks(criteria);
    }

    public async Task<int> CreateBookAsync(
        string title,
        string author,
        string isbn,
        string genre,
        int? publicationYear,
        string description,
        Stream? coverImageStream,
        string? imageFileName)
    {
        ValidateBookMetadata(title, author, isbn);

        // Generate catalogue entry (implementation depends on Books table structure)
        // For now, this is a placeholder - actual implementation would call repository to create book
        throw new NotImplementedException("Book creation requires Books table repository implementation.");
    }

    public async Task<bool> UpdateBookMetadataAsync(
        int bookId,
        string title,
        string author,
        string isbn,
        string genre,
        int? publicationYear,
        string description)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateBookMetadata(title, author, isbn);

        // Implementation would update book in repository
        throw new NotImplementedException("Book update requires Books table repository implementation.");
    }

    public async Task<string> UploadCoverImageAsync(int bookId, Stream imageStream, string fileName)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateCoverImage(imageStream, fileName);

        // Generate unique filename
        string uniqueFileName = $"{bookId}_{DateTime.UtcNow.Ticks}{Path.GetExtension(fileName)}";
        string filePath = Path.Combine(_imageUploadPath, uniqueFileName);
        string fullPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", filePath);

        try
        {
            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            // Save file to disk
            await using (var fileStream = new FileStream(fullPath, FileMode.Create))
            {
                await imageStream.CopyToAsync(fileStream);
            }

            // Return relative path for database storage
            return filePath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to upload cover image.", ex);
        }
    }

    public bool DeleteCoverImage(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;

        try
        {
            string fullPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", imagePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public Task<List<int>> AddCopiesToBookAsync(
        int bookId,
        int copyCount,
        string condition,
        string location)
    {
        ValidateId(bookId, nameof(bookId));
        ValidateCondition(condition);

        return _bookCopyRepository.CreateCopiesAsync(bookId, copyCount, condition, location);
    }

    public Task<List<BookCopy>> GetBookCopiesAsync(int bookId)
    {
        ValidateId(bookId, nameof(bookId));
        return _bookCopyRepository.GetCopiesByBookIdAsync(bookId);
    }

    public async Task<BookCopySummary> GetBookCopySummaryAsync(int bookId)
    {
        ValidateId(bookId, nameof(bookId));

        var copies = await _bookCopyRepository.GetCopiesByBookIdAsync(bookId);

        return new BookCopySummary
        {
            BookId = bookId,
            TotalCopies = copies.Count,
            AvailableCopies = copies.Count(c => c.Status == "Available"),
            CheckedOutCopies = copies.Count(c => c.Status == "Checked Out"),
            DamagedCopies = copies.Count(c => c.Status == "Damaged"),
            LostCopies = copies.Count(c => c.Status == "Lost"),
            RemovedCopies = copies.Count(c => c.Status == "Removed")
        };
    }

    //Validation Helpers

    private static void ValidateId(int id, string paramName)
    {
        if (id <= 0)
            throw new ArgumentException($"{paramName} must be greater than 0", paramName);
    }

    private static void ValidateBookMetadata(string title, string author, string isbn)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Book title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author name is required.", nameof(author));

        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN is required.", nameof(isbn));
    }

    private static void ValidateCondition(string condition)
    {
        var validConditions = new[] { "Like New", "Good", "Fair", "Poor" };
        if (!validConditions.Contains(condition))
            throw new ArgumentException("Invalid condition value.", nameof(condition));
    }

    private static void ValidateCoverImage(Stream? imageStream, string? fileName)
    {
        if (imageStream == null || imageStream.Length == 0)
            throw new ArgumentException("Cover image stream is required.", nameof(imageStream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Image file name is required.", nameof(fileName));

        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (imageStream.Length > maxFileSize)
            throw new ArgumentException("Cover image must be less than 5MB.", nameof(imageStream));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        string extension = Path.GetExtension(fileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("Cover image must be a valid image format (JPEG, PNG, GIF, or WebP).", nameof(fileName));
    }
}

//Supporting DTOs

public class BookCopySummary
{
    public int BookId { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int CheckedOutCopies { get; set; }
    public int DamagedCopies { get; set; }
    public int LostCopies { get; set; }
    public int RemovedCopies { get; set; }
}
