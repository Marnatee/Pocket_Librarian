using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.BLL.Services;
using Pocket_Librarian.Server.Services;

namespace Pocket_Librarian.Server.Controllers;

[ApiController]
[Route("api/donations")]
public sealed class BookDonationController : ControllerBase
{
    private static readonly HashSet<string> AcceptedImageTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    private readonly BookDonationService _service;
    private readonly CoverImageStorage _imageStorage;

    public BookDonationController(BookDonationService service, CoverImageStorage imageStorage)
    {
        _service = service;
        _imageStorage = imageStorage;
    }

    [Authorize(Roles = "Member,Staff")]
    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Submit([FromForm] SubmitBookDonationForm form)
    {
        int? userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "A signed-in member or staff account is required." });
        }

        if (form.CoverImage is not null
            && form.CoverImage.Length > 0
            && !AcceptedImageTypes.Contains(form.CoverImage.ContentType))
        {
            return BadRequest(new { message = "Cover images must be JPEG, PNG, or WebP files." });
        }

        string? coverImagePath = null;
        try
        {
            coverImagePath = await _imageStorage.SaveAsync(form.CoverImage);
            int donationId = await _service.SubmitAsync(new BookDonationSubmission
            {
                UserId = userId.Value,
                Title = form.Title ?? string.Empty,
                Author = form.Author,
                ISBN = form.ISBN,
                Genre = form.Genre,
                PublicationYear = form.PublicationYear,
                Description = form.Description,
                BookCondition = form.BookCondition,
                NumberOfCopies = form.NumberOfCopies,
                CoverImageContentType = form.CoverImage?.ContentType,
                CoverImageFileName = form.CoverImage?.FileName,
                CoverImagePath = coverImagePath,
                SourceType = "Member donation",
                DonationComments = form.DonationComments
            });

            return Created(
                $"/api/donations/{donationId}",
                new
                {
                    donationId,
                    status = "Pending Verification",
                    message = "Donation submitted for staff verification."
                });
        }
        catch (ArgumentException exception)
        {
            _imageStorage.Delete(coverImagePath);
            return BadRequest(new { message = exception.Message });
        }
        catch (SqlException)
        {
            _imageStorage.Delete(coverImagePath);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The donation service could not reach the database." });
        }
        catch
        {
            _imageStorage.Delete(coverImagePath);
            throw;
        }
    }

    [Authorize(Roles = "Staff")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        try
        {
            return Ok(await _service.GetPendingAsync());
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The donation service could not reach the database." });
        }
    }

    [Authorize(Roles = "Staff")]
    [HttpPost("{donationId:int}/approve")]
    public async Task<IActionResult> Approve(
        int donationId,
        [FromBody] BookDonationReview? review)
    {
        int? staffUserId = GetCurrentUserId();
        if (staffUserId is null)
        {
            return Unauthorized();
        }

        try
        {
            bool approved = await _service.ApproveAsync(
                donationId,
                staffUserId.Value,
                review?.ExistingBookId,
                review?.ReviewNotes);

            return approved
                ? Ok(new { message = "Donation approved and added to the catalogue." })
                : Conflict(new { message = "The donation could not be approved." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The donation service could not reach the database." });
        }
    }

    [Authorize(Roles = "Staff")]
    [HttpPost("{donationId:int}/reject")]
    public async Task<IActionResult> Reject(
        int donationId,
        [FromBody] BookDonationReview? review)
    {
        int? staffUserId = GetCurrentUserId();
        if (staffUserId is null)
        {
            return Unauthorized();
        }

        try
        {
            bool rejected = await _service.RejectAsync(
                donationId,
                staffUserId.Value,
                review?.ReviewNotes);

            return rejected
                ? Ok(new { message = "Donation rejected." })
                : Conflict(new { message = "The donation could not be rejected." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (SqlException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The donation service could not reach the database." });
        }
    }

    private int? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }
}

public sealed class SubmitBookDonationForm
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? Genre { get; set; }
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? BookCondition { get; set; }
    public int NumberOfCopies { get; set; } = 1;
    public IFormFile? CoverImage { get; set; }
    public string? DonationComments { get; set; }
}
