using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;
using System.Security.Claims;

namespace Pocket_Librarian.Server.Controllers;

[ApiController]
[Route("api/donations")]
public class DonationController : ControllerBase
{
    private readonly DonationRepository _donationRepository;

    public DonationController(
        DonationRepository donationRepository)
    {
        _donationRepository = donationRepository;
    }

    // Only authenticated members can submit donations.
    // Guests can still browse catalogue endpoints that are not protected.
    [Authorize(Roles = "Member")]
    [HttpPost]
    public async Task<IActionResult> SubmitDonation(
        [FromBody] CreateDonationRequest request)
    {
        // Gets the username from the authenticated account.
        string? username =
            User.Identity?.Name
            ?? User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(
                "A logged-in member account is required.");
        }

        try
        {
            // Saves the donation and connects it to the member.
            int donationId =
                await _donationRepository.SubmitDonationAsync(
                    username,
                    request);

            return Ok(new
            {
                message =
                    "Donation submitted for verification.",

                donationId,

                status =
                    "Pending Verification"
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception)
        {
            return StatusCode(
                500,
                "An error occurred while submitting the donation.");
        }
    }
}