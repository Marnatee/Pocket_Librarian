namespace Pocket_Librarian.BLL.Models
{

    // Represents the action taken by staff on a returned book in the returns queue.
    // Four actions are supported: Complete, AnotherBook, BadCondition, Other.

    public class ReturnAction
    {

        // The action type: "Complete", "AnotherBook", "BadCondition", or "Other".

        public string ActionType { get; set; } = string.Empty;


        // For ActionType "AnotherBook": the catalogue ID of the book found in this location.

        public int? FoundBookId { get; set; }


        // For ActionType "AnotherBook": the name of the book found in this location.

        public string? FoundBookTitle { get; set; }


        // For ActionType "AnotherBook": the shelf location for the found book.

        public string? FoundBookShelfLocation { get; set; }


        // For ActionType "Other": the support ticket description.

        public string? SupportTicketDescription { get; set; }


        // When this action was recorded.
 
        public DateTime DateActioned { get; set; } = DateTime.UtcNow;


        // The staff member who performed this action (FK to Users table).

        public int StaffId { get; set; }
    }
}
