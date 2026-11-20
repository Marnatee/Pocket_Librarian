using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;


// Repository interface for returns queue operations.
// Defines the contract for data access operations on returned books.

public interface IReturnsQueueRepository
{

    // Get all active return tasks assigned to a specific staff member.
    Task<List<ReturnTask>> GetPendingTasksForStaffAsync(int staffId);


    // Get all pending return tasks (not filtered by staff).

    Task<List<ReturnTask>> GetAllPendingTasksAsync();


    // Get a specific return task by ID.

    Task<ReturnTask?> GetReturnTaskByIdAsync(int returnTaskId);

    // Create a new return task when a book is returned.

    Task<int> CreateReturnTaskAsync(ReturnTask task);


    // Atomically claim the oldest unassigned task.
    Task<ReturnTask?> ClaimNextTaskAsync(int staffId);

    // Atomically claim a specific unassigned task.
    Task<ReturnTask?> ClaimTaskAsync(int returnTaskId, int staffId);


    // Process a return action in one database transaction.
    Task<ReturnActionResponse> ProcessReturnActionAsync(
        int returnTaskId,
        int staffId,
        ReturnAction action,
        SupportTicket? supportTicket = null);


    // Get a support ticket by ID.

    Task<SupportTicket?> GetSupportTicketByIdAsync(int ticketId);


    // Get all support tickets (optionally filtered by status).

    Task<List<SupportTicket>> GetSupportTicketsAsync(string? status = null);

    // Mark a support ticket resolved and preserve the resolution notes.
    Task<bool> ResolveSupportTicketAsync(int ticketId, int staffId, string resolutionNotes);
}
