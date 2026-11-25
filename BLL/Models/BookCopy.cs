namespace Pocket_Librarian.BLL.Models
{
    public class BookCopy
    {
        public int CopyId { get; set; }

        public int BookId { get; set; }

        public string? Barcode { get; set; }

        public string Condition { get; set; } = "Good";

        public string Status { get; set; } = "Available";

        public string? Location { get; set; }

        public int? CurrentReservationId { get; set; }

        public DateTime? CheckoutDueDate { get; set; }

        public DateTime DateAcquired { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        public DateTime DateModified { get; set; } = DateTime.UtcNow;
    }
}
