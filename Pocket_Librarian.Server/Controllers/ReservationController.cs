// API Controller for Reservation endpoints
// Handles HTTP requests related to reservations and delegates to the service layer

using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Services;

namespace Pocket_Librarian.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationController : ControllerBase
    {
        private readonly ReservationService _service;

        public ReservationController(ReservationService service)
        {
            _service = service;
        }

        
        [HttpPost("create")]
        public async Task<ActionResult<object>> CreateReservation([FromBody] CreateReservationRequest request)
        {
            try
            {
                if (request?.UserId <= 0 || request?.BookId <= 0)
                {
                    return BadRequest(new { error = "UserId and BookId must be greater than 0" });
                }

                int reservationId = await _service.CreateReservationAsync(request.UserId, request.BookId);

                return Ok(new
                {
                    success = true,
                    reservationId = reservationId,
                    message = "Reservation created successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the reservation", details = ex.Message });
            }
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<Reservation>> GetReservation(int id)
        {
            try
            {
                var reservation = await _service.GetReservationByIdAsync(id);

                if (reservation == null)
                {
                    return NotFound(new { error = $"Reservation with ID {id} not found" });
                }

                return Ok(reservation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the reservation", details = ex.Message });
            }
        }

  
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Reservation>>> GetUserReservations(int userId)
        {
            try
            {
                var reservations = await _service.GetUserReservationHistoryAsync(userId);
                return Ok(reservations);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user reservations", details = ex.Message });
            }
        }

        
        [HttpGet("user/{userId}/active")]
        public async Task<ActionResult<List<Reservation>>> GetUserActiveReservations(int userId)
        {
            try
            {
                var reservations = await _service.GetUserActiveReservationsAsync(userId);
                return Ok(reservations);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving active reservations", details = ex.Message });
            }
        }

    
        [HttpGet("book/{bookId}")]
        public async Task<ActionResult<List<Reservation>>> GetBookReservations(int bookId)
        {
            try
            {
                var reservations = await _service.GetBookReservationHistoryAsync(bookId);
                return Ok(reservations);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving book reservations", details = ex.Message });
            }
        }

       
        [HttpGet("active/all")]
        public async Task<ActionResult<List<Reservation>>> GetAllActiveReservations()
        {
            try
            {
                var reservations = await _service.GetAllActiveReservationsAsync();
                return Ok(reservations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving active reservations", details = ex.Message });
            }
        }

    
        [HttpPut("{id}/return")]
        public async Task<ActionResult<object>> ReturnBook(int id)
        {
            try
            {
                bool success = await _service.ReturnBookAsync(id);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Book returned successfully"
                    });
                }
                else
                {
                    return StatusCode(500, new { error = "Failed to return book" });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while returning the book", details = ex.Message });
            }
        }

    
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateReservation(int id, [FromBody] Reservation reservation)
        {
            try
            {
                if (reservation == null || reservation.ReservationId != id)
                {
                    return BadRequest(new { error = "Reservation ID mismatch" });
                }

                bool success = await _service.UpdateReservationAsync(reservation);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Reservation updated successfully"
                    });
                }
                else
                {
                    return NotFound(new { error = "Reservation not found" });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the reservation", details = ex.Message });
            }
        }

    
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteReservation(int id)
        {
            try
            {
                bool success = await _service.DeleteReservationAsync(id);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Reservation deleted successfully"
                    });
                }
                else
                {
                    return NotFound(new { error = "Reservation not found" });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the reservation", details = ex.Message });
            }
        }

    
        [HttpGet("book/{bookId}/available")]
        public async Task<ActionResult<object>> IsBookAvailable(int bookId)
        {
            try
            {
                bool available = await _service.IsBookAvailableAsync(bookId);
                return Ok(new { bookId = bookId, available = available });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while checking book availability", details = ex.Message });
            }
        }
    }

   
    public class CreateReservationRequest
    {
        public int UserId { get; set; }
        public int BookId { get; set; }
    }
}
