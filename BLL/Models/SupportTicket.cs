namespace Pocket_Librarian.BLL.Models
{

    // Represents a support ticket created when staff selects "Other" for a returned book.

    public class SupportTicket
    {

        // Unique identifier for this support ticket.

        public int SupportTicketId { get; set; }


        // The return task that prompted this ticket (FK to ReturnTasks table).

        public int ReturnTaskId { get; set; }


        // The book associated with this support issue (FK to Books table).

        public int BookId { get; set; }


        // The staff member who created this ticket (FK to Users table).

        public int CreatedByStaffId { get; set; }


        // Staff member currently responsible for the ticket.

        public int? AssignedStaffId { get; set; }


        // Description of the issue requiring support.

        public string Description { get; set; } = string.Empty;


        // Current status: "Open", "In Progress", "Resolved", "Closed".

        public string Status { get; set; } = "Open";


        // When this ticket was created.

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;


        // When this ticket was resolved (null if still open).

        public DateTime? DateResolved { get; set; }


        // Notes on the resolution.

        public string? ResolutionNotes { get; set; }
    }
}
