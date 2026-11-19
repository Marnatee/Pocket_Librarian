using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;


// Repository interface for returns queue operations.
// Defines the contract for data access operations on returned books.

public interface IReturnsQueueRepository
{

    // Get all pending return tasks assigned to a specific staff member.
    Task<List<ReturnTask>> GetPendingTasksForStaffAsync(int staffId);


    // Get all pending return tasks (not filtered by staff).

    Task<List<ReturnTask>> GetAllPendingTasksAsync();


    // Get a specific return task by ID.

    Task<ReturnTask?> GetReturnTaskByIdAsync(int returnTaskId);

    // Create a new return task when a book is returned.

    Task<int> CreateReturnTaskAsync(ReturnTask task);


    // Update a return task's status and completion date.

    Task<bool> UpdateReturnTaskAsync(ReturnTask task);


    // Mark a return task as completed.

    Task<bool> CompleteReturnTaskAsync(int returnTaskId);


    // Record a return action (Complete, AnotherBook, BadCondition, Other).

    Task<int> RecordReturnActionAsync(ReturnAction action, int returnTaskId);


    // Create a support ticket for a return task flagged as "Other".

    Task<int> CreateSupportTicketAsync(SupportTicket ticket);


    // Get a support ticket by ID.

    Task<SupportTicket?> GetSupportTicketByIdAsync(int ticketId);


    // Flag a book for deletion in the database.
    // Called when "BadCondition" action is selected.

    Task<bool> FlagBookForDeletionAsync(int bookId, string reason);


    // Get all support tickets (optionally filtered by status).

    Task<List<SupportTicket>> GetSupportTicketsAsync(string? status = null);
}
