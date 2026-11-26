using Microsoft.AspNetCore.Http;

namespace Pocket_Librarian.Server.Services;

public sealed class CoverImageStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private readonly IWebHostEnvironment _environment;

    private static readonly IReadOnlyDictionary<string, string> Extensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    public CoverImageStorage(IWebHostEnvironment environment) => _environment = environment;

    public async Task<string?> SaveAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > MaxBytes) throw new ArgumentException("Cover image cannot exceed 5 MB.");
        if (!Extensions.TryGetValue(file.ContentType, out string? extension))
            throw new ArgumentException("Cover images must be JPEG, PNG, or WebP files.");

        string webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        string directory = Path.Combine(webRoot, "uploads", "covers");
        Directory.CreateDirectory(directory);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string fullPath = Path.Combine(directory, fileName);
        await using FileStream output = new(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(output);
        return $"/uploads/covers/{fileName}";
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        string normalized = relativePath.Replace('\\', '/');
        if (!normalized.StartsWith("/uploads/covers/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("..", StringComparison.Ordinal))
            return;

        string webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        string fileName = Path.GetFileName(normalized);
        string fullPath = Path.Combine(webRoot, "uploads", "covers", fileName);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}
