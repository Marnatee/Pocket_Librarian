namespace Pocket_Librarian.Server.Models
{
    public class Reservation
    {
        
        public int ReservationId { get; set; }

   
        public int UserId { get; set; }

        
        public int BookId { get; set; }

     
        public DateTime DateReserved { get; set; }

        
        public DateTime? DateReturned { get; set; }

        // Joined display values returned by reservation read queries.
        // These do not change the underlying Reservations table.
        public string? UserName { get; set; }

        public string? UserEmail { get; set; }

        public string? BookTitle { get; set; }
    }
}
