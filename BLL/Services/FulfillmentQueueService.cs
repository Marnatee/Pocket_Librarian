// Business Logic Layer.
// Validates fulfillment-queue commands and delegates persistence to the DAL.

using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public class FulfillmentQueueService
{
    private readonly IFulfillmentQueueRepository _repository;

    public FulfillmentQueueService(IFulfillmentQueueRepository repository)
    {
        _repository = repository;
    }


    // Get all pending fulfillment tasks (admin/overview).

    public Task<List<FulfillmentTask>> GetAllPendingTasksAsync() =>
        _repository.GetAllPendingTasksAsync();


    // Get all pending tasks assigned to a specific staff member.

    public Task<List<FulfillmentTask>> GetMyPendingTasksAsync(int staffId)
    {
        ValidateStaffId(staffId);
        return _repository.GetPendingTasksForStaffAsync(staffId);
    }


    // Get a specific fulfillment task by ID.

    public Task<FulfillmentTask?> GetFulfillmentTaskAsync(int fulfillmentTaskId)
    {
        ValidateId(fulfillmentTaskId, nameof(fulfillmentTaskId));
        return _repository.GetFulfillmentTaskByIdAsync(fulfillmentTaskId);
    }


    // Create a new fulfillment task when a reservation is made.
    // Called by ReservationService during reservation creation.

    public async Task<int> CreateFulfillmentTaskAsync(
        int reservationId,
        int userId,
        int bookId,
        string bookTitle,
        string memberName)
    {
        ValidatePositiveId(reservationId, nameof(reservationId));
        ValidatePositiveId(userId, nameof(userId));
        ValidatePositiveId(bookId, nameof(bookId));

        if (string.IsNullOrWhiteSpace(bookTitle))
        {
            throw new ArgumentException("Book title is required.", nameof(bookTitle));
        }

        if (string.IsNullOrWhiteSpace(memberName))
        {
            throw new ArgumentException("Member name is required.", nameof(memberName));
        }

        return await _repository.CreateFulfillmentTaskAsync(new FulfillmentTask
        {
            ReservationId = reservationId,
            UserId = userId,
            BookId = bookId,
            BookTitle = bookTitle.Trim(),
            MemberName = memberName.Trim(),
            Status = "Pending",
            DateCreated = DateTime.UtcNow
        });
    }


    // Claim the next (oldest) unassigned fulfillment task.

    public Task<FulfillmentTask?> ClaimNextTaskAsync(int staffId)
    {
        ValidateStaffId(staffId);
        return _repository.ClaimNextTaskAsync(staffId);
    }


    // Claim a specific unassigned fulfillment task.

    public Task<FulfillmentTask?> ClaimTaskAsync(int fulfillmentTaskId, int staffId)
    {
        ValidateCommand(fulfillmentTaskId, staffId);
        return _repository.ClaimTaskAsync(fulfillmentTaskId, staffId);
    }


    // Mark a fulfillment task as fulfilled (book located and ready for pickup).
    // The repository updates the reservation and creates the notification in
    // the same SQL transaction, so fulfillment cannot silently succeed without
    // the member notification.

    public async Task<FulfillmentTask?> MarkTaskFulfilledAsync(int fulfillmentTaskId, int staffId)
    {
        ValidateCommand(fulfillmentTaskId, staffId);
        FulfillmentTask? task = await _repository.MarkTaskFulfilledAsync(fulfillmentTaskId, staffId);
        if (task is null)
        {
            return null;
        }

        return task;
    }


    // Get all completed fulfillment tasks for history/reporting.

    public Task<List<FulfillmentTask>> GetCompletedTasksAsync() =>
        _repository.GetCompletedTasksAsync();

    // Validation helpers
    private static void ValidateStaffId(int staffId)
    {
        if (staffId <= 0)
            throw new ArgumentException("Staff ID must be greater than 0", nameof(staffId));
    }

    private static void ValidatePositiveId(int id, string paramName)
    {
        if (id <= 0)
            throw new ArgumentException($"{paramName} must be greater than 0", paramName);
    }

    private static void ValidateId(int id, string paramName)
    {
        if (id <= 0)
            throw new ArgumentException($"{paramName} must be greater than 0", paramName);
    }

    private static void ValidateCommand(int taskId, int staffId)
    {
        ValidateId(taskId, nameof(taskId));
        ValidateStaffId(staffId);
    }
}
