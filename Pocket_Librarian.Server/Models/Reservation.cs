namespace Pocket_Librarian.Server.Models
{
    public class Reservation
    {
        
        public int ReservationId { get; set; }

   
        public int UserId { get; set; }

        
        public int BookId { get; set; }

     
        public DateTime DateReserved { get; set; }

        
        public DateTime? DateReturned { get; set; }
    }
}
