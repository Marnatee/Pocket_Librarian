using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.BLL.Services;
using Pocket_Librarian.Server.Services;

namespace Pocket_Librarian.Server.Controllers;

[ApiController]
[Authorize(Roles = "Staff")]
[Route("api/catalogue-admin")]
public sealed class CatalogueAdministrationController : ControllerBase
{
    private readonly DonationService _donationService;
    private readonly CatalogueAdminService _catalogueService;
    private readonly CoverImageStorage _imageStorage;

    public CatalogueAdministrationController(
        DonationService donationService,
        CatalogueAdminService catalogueService,
        CoverImageStorage imageStorage)
    {
        _donationService = donationService;
        _catalogueService = catalogueService;
        _imageStorage = imageStorage;
    }

    [HttpGet("donations")]
    public async Task<IActionResult> GetDonations([FromQuery] string status = "Pending")
    {
        try
        {
            return Ok(await _donationService.GetDonationsByStatusAsync(status));
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpGet("donations/{donationId:int}")]
    public async Task<IActionResult> GetDonation(int donationId)
    {
        try
        {
            Donation? donation = await _donationService.GetDonationByIdAsync(donationId);
            return donation is null ? NotFound(new { message = "Donation not found." }) : Ok(donation);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpPut("donations/{donationId:int}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UpdateDonation(
        int donationId,
        [FromForm] DonationEditForm form)
    {
        string? newImagePath = null;
        try
        {
            Donation? current = await _donationService.GetDonationByIdAsync(donationId);
            if (current is null) return NotFound(new { message = "Donation not found." });

            newImagePath = await _imageStorage.SaveAsync(form.CoverImage);
            bool updated = await _donationService.UpdateDonationAsync(donationId, new()
            {
                Title = form.Title ?? string.Empty,
                Author = form.Author,
                ISBN = form.ISBN,
                Genre = form.Genre,
                PublicationYear = form.PublicationYear,
                Description = form.Description,
                Condition = string.IsNullOrWhiteSpace(form.Condition) ? "Good" : form.Condition,
                CopiesDonated = form.CopiesDonated,
                SourceType = string.IsNullOrWhiteSpace(form.SourceType) ? "Member donation" : form.SourceType,
                CoverImagePath = newImagePath ?? current.CoverImagePath,
                DonationComments = form.DonationComments,
                SubmittedByUserId = current.SubmittedByUserId,
                DateSubmitted = current.DateSubmitted
            });

            if (!updated)
            {
                _imageStorage.Delete(newImagePath);
                return Conflict(new { message = "The donation was not updated." });
            }
            if (newImagePath is not null) _imageStorage.Delete(current.CoverImagePath);
            return Ok(new { message = "Donation updated." });
        }
        catch (ArgumentException exception)
        {
            _imageStorage.Delete(newImagePath);
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            _imageStorage.Delete(newImagePath);
            return Conflict(new { message = exception.Message });
        }
        catch (SqlException)
        {
            _imageStorage.Delete(newImagePath);
            return DatabaseUnavailable();
        }
    }

    [HttpPost("donations/{donationId:int}/verify")]
    public Task<IActionResult> VerifyDonation(int donationId, [FromBody] ReviewRequest? request) =>
        RunDonationActionAsync(() => _donationService.VerifyDonationAsync(donationId, CurrentStaffId(), request?.Notes));

    [HttpPost("donations/{donationId:int}/approve")]
    public Task<IActionResult> ApproveDonation(int donationId, [FromBody] ApproveRequest? request) =>
        RunDonationActionAsync(() => _donationService.ApproveDonationAsync(
            donationId,
            request?.ExistingBookId,
            CurrentStaffId(),
            request?.Notes));

    [HttpPost("donations/{donationId:int}/reject")]
    public Task<IActionResult> RejectDonation(int donationId, [FromBody] ReviewRequest? request) =>
        RunDonationActionAsync(() => _donationService.RejectDonationAsync(donationId, CurrentStaffId(), request?.Notes));

    [HttpPost("donations/{donationId:int}/archive")]
    public Task<IActionResult> ArchiveDonation(int donationId, [FromBody] ReviewRequest? request) =>
        RunDonationActionAsync(() => _donationService.ArchiveDonationAsync(donationId, CurrentStaffId(), request?.Notes));

    [HttpDelete("donations/{donationId:int}")]
    public async Task<IActionResult> DeleteDonation(int donationId)
    {
        try
        {
            Donation? current = await _donationService.GetDonationByIdAsync(donationId);
            if (current is null) return NotFound(new { message = "Donation not found." });

            bool deleted = await _donationService.DeleteDonationAsync(donationId);
            if (deleted) _imageStorage.Delete(current.CoverImagePath);
            return deleted
                ? Ok(new { message = "Donation deleted." })
                : Conflict(new { message = "Approved donations cannot be deleted." });
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpGet("books")]
    public async Task<IActionResult> GetBooks()
    {
        try { return Ok(await _catalogueService.GetBooksAsync()); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpGet("books/{bookId:int}")]
    public async Task<IActionResult> GetBook(int bookId)
    {
        try
        {
            CatalogueBook? book = await _catalogueService.GetBookAsync(bookId);
            return book is null ? NotFound(new { message = "Book not found." }) : Ok(book);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpPost("books")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> CreateBook([FromForm] CatalogueBookForm form)
    {
        string? imagePath = null;
        try
        {
            imagePath = await _imageStorage.SaveAsync(form.CoverImage);
            int bookId = await _catalogueService.CreateBookAsync(ToMutation(form, imagePath));
            return Created($"/api/catalogue-admin/books/{bookId}", new { bookId });
        }
        catch (ArgumentException exception)
        {
            _imageStorage.Delete(imagePath);
            return BadRequest(new { message = exception.Message });
        }
        catch (SqlException)
        {
            _imageStorage.Delete(imagePath);
            return DatabaseUnavailable();
        }
    }

    [HttpPut("books/{bookId:int}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UpdateBook(int bookId, [FromForm] CatalogueBookForm form)
    {
        string? imagePath = null;
        try
        {
            CatalogueBook? current = await _catalogueService.GetBookAsync(bookId);
            if (current is null) return NotFound(new { message = "Book not found." });
            imagePath = await _imageStorage.SaveAsync(form.CoverImage);
            bool updated = await _catalogueService.UpdateBookAsync(bookId, ToMutation(form, imagePath));
            if (!updated)
            {
                _imageStorage.Delete(imagePath);
                return NotFound();
            }
            if (imagePath is not null) _imageStorage.Delete(current.CoverImagePath);
            return Ok(new { message = "Catalogue record updated." });
        }
        catch (ArgumentException exception)
        {
            _imageStorage.Delete(imagePath);
            return BadRequest(new { message = exception.Message });
        }
        catch (SqlException)
        {
            _imageStorage.Delete(imagePath);
            return DatabaseUnavailable();
        }
    }

    [HttpDelete("books/{bookId:int}")]
    public async Task<IActionResult> DeleteBook(int bookId)
    {
        try
        {
            CatalogueBook? current = await _catalogueService.GetBookAsync(bookId);
            if (current is null) return NotFound(new { message = "Book not found." });
            bool deleted = await _catalogueService.DeleteBookAsync(bookId);
            if (deleted) _imageStorage.Delete(current.CoverImagePath);
            return deleted ? Ok(new { message = "Catalogue record deleted." }) : Conflict(new { message = "Book could not be deleted." });
        }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpGet("books/{bookId:int}/copies")]
    public async Task<IActionResult> GetCopies(int bookId)
    {
        try { return Ok(await _catalogueService.GetCopiesAsync(bookId)); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpPost("books/{bookId:int}/copies")]
    public async Task<IActionResult> AddCopies(int bookId, [FromBody] AddCopiesRequest request)
    {
        try
        {
            List<int> ids = await _catalogueService.AddCopiesAsync(
                bookId, request.Count, request.Condition, request.Location);
            return Ok(new { copyIds = ids });
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpPut("copies/{copyId:int}")]
    public async Task<IActionResult> UpdateCopy(int copyId, [FromBody] UpdateCopyRequest request)
    {
        try
        {
            bool updated = await _catalogueService.UpdateCopyAsync(
                copyId, request.Condition, request.Location, request.Notes, request.Status);
            return updated ? Ok(new { message = "Copy updated." }) : NotFound(new { message = "Copy not found." });
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    [HttpDelete("copies/{copyId:int}")]
    public async Task<IActionResult> DeleteCopy(int copyId)
    {
        try
        {
            bool deleted = await _catalogueService.DeleteCopyAsync(copyId);
            return deleted ? Ok(new { message = "Copy deleted." }) : Conflict(new { message = "Copy could not be deleted. Checked-out copies must be returned first." });
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    private async Task<IActionResult> RunDonationActionAsync(Func<Task<bool>> action)
    {
        try
        {
            bool succeeded = await action();
            return succeeded ? Ok(new { message = "Donation action completed." }) : Conflict(new { message = "The donation action could not be completed." });
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
        catch (SqlException) { return DatabaseUnavailable(); }
    }

    private int CurrentStaffId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int id) ? id : throw new InvalidOperationException("A staff identity is required.");
    }

    private static CatalogueBookMutation ToMutation(CatalogueBookForm form, string? imagePath) =>
        new()
        {
            Title = form.Title ?? string.Empty,
            Author = form.Author,
            ISBN = form.ISBN,
            Genre = form.Genre,
            PublicationYear = form.PublicationYear,
            Description = form.Description,
            CoverImagePath = imagePath,
            SourceType = string.IsNullOrWhiteSpace(form.SourceType) ? "Library acquisition" : form.SourceType
        };

    private ObjectResult DatabaseUnavailable() =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The catalogue service could not reach the database." });
}

public sealed class ReviewRequest
{
    public string? Notes { get; set; }
}

public sealed class ApproveRequest
{
    public int? ExistingBookId { get; set; }
    public string? Notes { get; set; }
}

public sealed class DonationEditForm
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? Genre { get; set; }
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public int CopiesDonated { get; set; } = 1;
    public string? SourceType { get; set; }
    public string? DonationComments { get; set; }
    public IFormFile? CoverImage { get; set; }
}

public sealed class CatalogueBookForm
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? Genre { get; set; }
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? SourceType { get; set; }
    public IFormFile? CoverImage { get; set; }
}

public sealed class AddCopiesRequest
{
    public int Count { get; set; } = 1;
    public string Condition { get; set; } = "Good";
    public string? Location { get; set; }
}

public sealed class UpdateCopyRequest
{
    public string Condition { get; set; } = "Good";
    public string? Status { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}
