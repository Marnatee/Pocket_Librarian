namespace Pocket_Librarian.BLL.Models
{
    public class Reservation
    {
        
        public int ReservationId { get; set; }

   
        public int UserId { get; set; }

        
        public int BookId { get; set; }

     
        public DateTime DateReserved { get; set; }

        
        public DateTime? DateReturned { get; set; }

        // Pending means the member requested the book and staff has not yet
        // located it. Fulfilled means it is ready/checked out. Returned means
        // the member has returned it and the returns queue is processing it.
        public string ReservationStatus { get; set; } = "Pending";

        // Joined display values returned by reservation read queries.
        // These do not change the underlying Reservations table.
        public string? UserName { get; set; }

        public string? UserEmail { get; set; }

        public string? BookTitle { get; set; }
    }
}
