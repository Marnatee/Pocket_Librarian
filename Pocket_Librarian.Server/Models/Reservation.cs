namespace Pocket_Librarian.Server.Models;

// Represents one reservation returned to the member
public class Reservation
{
    // Unique ID for the reservation
    public int ReservationId { get; set; }

    // ID of the member who made the reservation
    public int UserId { get; set; }

    // Title of the reserved book
    public string BookTitle { get; set; } = "";

    // Date and time the reservation was created
    public DateTime ReservationDate { get; set; }

    // Current status, such as Current or Completed
    public string Status { get; set; } = "";
}