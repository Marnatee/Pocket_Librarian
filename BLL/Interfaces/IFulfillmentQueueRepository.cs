using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Interfaces;


// Repository interface for fulfillment queue operations.
// Defines the contract for data access operations on reserved books awaiting fulfillment.

public interface IFulfillmentQueueRepository
{


    Task<List<FulfillmentTask>> GetAllPendingTasksAsync();

    Task<List<FulfillmentTask>> GetPendingTasksForStaffAsync(int staffId);

    Task<FulfillmentTask?> GetFulfillmentTaskByIdAsync(int fulfillmentTaskId);

    Task<int> CreateFulfillmentTaskAsync(FulfillmentTask task);

    Task<FulfillmentTask?> ClaimNextTaskAsync(int staffId);

    Task<FulfillmentTask?> ClaimTaskAsync(int fulfillmentTaskId, int staffId);

    Task<FulfillmentTask?> MarkTaskFulfilledAsync(int fulfillmentTaskId, int staffId);

    Task<List<FulfillmentTask>> GetCompletedTasksAsync();
}
