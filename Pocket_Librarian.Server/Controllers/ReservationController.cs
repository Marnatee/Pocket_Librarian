using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;

namespace Pocket_Librarian.Server.Controllers;

// Identifies this class as an API controller
[ApiController]

// Sets the base URL for reservation requests
[Route("api/reservations")]
public class ReservationController : ControllerBase
{
    // Provides access to reservation database operations
    private readonly ReservationRepository _repository;

    // Receives the repository through dependency injection
    public ReservationController(
        ReservationRepository repository)
    {
        _repository = repository;
    }

    // Retrieves current reservations for a member
    //
    // Example request:
    // GET /api/reservations/GregBooks
    [HttpGet("{username}")]
    public async Task<ActionResult<List<Reservation>>>
        GetCurrentReservations(string username)
    {
        // Requests the reservations from the repository
        List<Reservation> reservations =
            await _repository
                .GetCurrentReservationsAsync(
                    username);

        // Returns the reservations as JSON
        return Ok(reservations);
    }
}