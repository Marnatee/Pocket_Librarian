namespace Pocket_Librarian.BLL.Models
{

    public class FulfillmentTask
    {

        public int FulfillmentTaskId { get; set; }

        public int ReservationId { get; set; }

        public int UserId { get; set; }

        public int BookId { get; set; }

        public int? AssignedStaffId { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        public string MemberName { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? DateAssigned { get; set; }

        public DateTime? DateFulfilled { get; set; }
    }
}
