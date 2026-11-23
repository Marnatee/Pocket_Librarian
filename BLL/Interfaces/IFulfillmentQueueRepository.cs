using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;

/// <summary>
/// Repository interface for fulfillment queue operations.
/// Defines the contract for data access operations on reserved books awaiting fulfillment.
/// </summary>
public interface IFulfillmentQueueRepository
{
    /// <summary>
    /// Get all pending fulfillment tasks (not filtered by staff).
    /// </summary>
    Task<List<FulfillmentTask>> GetAllPendingTasksAsync();

    /// <summary>
    /// Get all fulfillment tasks assigned to a specific staff member.
    /// </summary>
    Task<List<FulfillmentTask>> GetPendingTasksForStaffAsync(int staffId);

    /// <summary>
    /// Get a specific fulfillment task by ID.
    /// </summary>
    Task<FulfillmentTask?> GetFulfillmentTaskByIdAsync(int fulfillmentTaskId);

    /// <summary>
    /// Create a new fulfillment task when a reservation is made.
    /// </summary>
    Task<int> CreateFulfillmentTaskAsync(FulfillmentTask task);

    /// <summary>
    /// Atomically claim the oldest unassigned task for a staff member.
    /// </summary>
    Task<FulfillmentTask?> ClaimNextTaskAsync(int staffId);

    /// <summary>
    /// Atomically claim a specific unassigned task for a staff member.
    /// </summary>
    Task<FulfillmentTask?> ClaimTaskAsync(int fulfillmentTaskId, int staffId);

    /// <summary>
    /// Mark a fulfillment task as fulfilled (ready for pickup).
    /// Creates a notification for the member and returns the task with updated status.
    /// </summary>
    Task<FulfillmentTask?> MarkTaskFulfilledAsync(int fulfillmentTaskId, int staffId);

    /// <summary>
    /// Get all completed fulfillment tasks for history purposes.
    /// </summary>
    Task<List<FulfillmentTask>> GetCompletedTasksAsync();
}
