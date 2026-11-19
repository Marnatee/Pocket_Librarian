namespace Pocket_Librarian.BLL.Models
{

    // Response returned to the client after processing a return action.

    public class ReturnActionResponse
    {

        // Whether the action was processed successfully.

        public bool Success { get; set; }


        // Message describing the result of the action.

        public string Message { get; set; } = string.Empty;


        // The completed return task details.

        public ReturnTask? CompletedTask { get; set; }


        // If a new task was created (e.g., for "AnotherBook" action), include it here.

        public ReturnTask? NewTask { get; set; }


        // If a support ticket was created (e.g., for "Other" action), include it here.

        public SupportTicket? SupportTicket { get; set; }
    }
}
