namespace Pocket_Librarian.BLL.Models
{

    // Represents a return queue task assigned to staff.
    // A staff member processes a returned book by selecting one of four actions.

    public class ReturnTask
    {

        // Unique identifier for this return task.

        public int ReturnTaskId { get; set; }


        // The reservation being returned (nullable for a found/mis-shelved book).

        public int? ReservationId { get; set; }


        // The book being returned (FK to Books table).

        public int BookId { get; set; }


        // The staff member assigned to this task (FK to Users table).

        public int? AssignedStaffId { get; set; }


        // The shelf location where this book should be restocked.

        public string ShelfLocation { get; set; } = "Unassigned";


        // The title of the book being returned (denormalized for display).

        public string BookTitle { get; set; } = string.Empty;


        // Current status of the task: "Pending", "InProgress", or "Completed".

        public string Status { get; set; } = "Pending";


        // When this task was created.

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;


        // When a staff member claimed the task.

        public DateTime? DateClaimed { get; set; }


        // When this task was completed (null if still pending).

        public DateTime? DateCompleted { get; set; }
    }
}
