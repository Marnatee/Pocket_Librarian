// Business Logic Layer.
// Validates returns-queue commands and delegates persistence to the DAL.

using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services;

public class ReturnsQueueService
{
    private readonly IReturnsQueueRepository _repository;

    public ReturnsQueueService(IReturnsQueueRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ReturnTask>> GetMyPendingTasksAsync(int staffId)
    {
        ValidateStaffId(staffId);
        return _repository.GetPendingTasksForStaffAsync(staffId);
    }

    public Task<List<ReturnTask>> GetAllPendingTasksAsync() => _repository.GetAllPendingTasksAsync();

    public Task<ReturnTask?> GetReturnTaskAsync(int returnTaskId)
    {
        ValidateId(returnTaskId, nameof(returnTaskId));
        return _repository.GetReturnTaskByIdAsync(returnTaskId);
    }

    public Task<ReturnTask?> ClaimNextTaskAsync(int staffId)
    {
        ValidateStaffId(staffId);
        return _repository.ClaimNextTaskAsync(staffId);
    }

    public Task<ReturnTask?> ClaimTaskAsync(int returnTaskId, int staffId)
    {
        ValidateCommand(returnTaskId, staffId);
        return _repository.ClaimTaskAsync(returnTaskId, staffId);
    }

    // Intended to be called by the reservation-return workflow.
    // New tasks are unassigned until a staff member claims them.
    public async Task<int> CreateReturnTaskAsync(
        int? reservationId,
        int bookId,
        string bookTitle,
        string shelfLocation = "Unassigned")
    {
        ValidatePositiveId(bookId, nameof(bookId));

        if (string.IsNullOrWhiteSpace(bookTitle))
        {
            throw new ArgumentException("Book title is required.", nameof(bookTitle));
        }

        return await _repository.CreateReturnTaskAsync(new ReturnTask
        {
            ReservationId = reservationId,
            BookId = bookId,
            BookTitle = bookTitle.Trim(),
            ShelfLocation = string.IsNullOrWhiteSpace(shelfLocation) ? "Unassigned" : shelfLocation.Trim(),
            Status = "Pending",
            DateCreated = DateTime.UtcNow
        });
    }

    public Task<ReturnActionResponse> HandleCompleteActionAsync(int returnTaskId, int staffId)
    {
        ValidateCommand(returnTaskId, staffId);
        return _repository.ProcessReturnActionAsync(returnTaskId, staffId, new ReturnAction
        {
            ActionType = "Complete",
            StaffId = staffId,
            DateActioned = DateTime.UtcNow
        });
    }

    public Task<ReturnActionResponse> HandleAnotherBookActionAsync(
        int returnTaskId,
        int staffId,
        int foundBookId,
        string foundBookTitle,
        string foundBookShelfLocation)
    {
        ValidateCommand(returnTaskId, staffId);
        ValidatePositiveId(foundBookId, nameof(foundBookId));

        if (string.IsNullOrWhiteSpace(foundBookTitle))
        {
            throw new ArgumentException("The found book title is required.", nameof(foundBookTitle));
        }

        if (string.IsNullOrWhiteSpace(foundBookShelfLocation))
        {
            throw new ArgumentException("The found book shelf location is required.", nameof(foundBookShelfLocation));
        }

        return _repository.ProcessReturnActionAsync(returnTaskId, staffId, new ReturnAction
        {
            ActionType = "AnotherBook",
            FoundBookId = foundBookId,
            FoundBookTitle = foundBookTitle.Trim(),
            FoundBookShelfLocation = foundBookShelfLocation.Trim(),
            StaffId = staffId,
            DateActioned = DateTime.UtcNow
        });
    }

    public Task<ReturnActionResponse> HandleBadConditionActionAsync(int returnTaskId, int staffId)
    {
        ValidateCommand(returnTaskId, staffId);
        return _repository.ProcessReturnActionAsync(returnTaskId, staffId, new ReturnAction
        {
            ActionType = "BadCondition",
            StaffId = staffId,
            DateActioned = DateTime.UtcNow
        });
    }

    public Task<ReturnActionResponse> HandleOtherActionAsync(int returnTaskId, int staffId, string ticketDescription)
    {
        ValidateCommand(returnTaskId, staffId);

        if (string.IsNullOrWhiteSpace(ticketDescription))
        {
            throw new ArgumentException("A support-ticket description is required.", nameof(ticketDescription));
        }

        var ticket = new SupportTicket
        {
            CreatedByStaffId = staffId,
            Description = ticketDescription.Trim(),
            Status = "Open",
            DateCreated = DateTime.UtcNow
        };

        return _repository.ProcessReturnActionAsync(returnTaskId, staffId, new ReturnAction
        {
            ActionType = "Other",
            SupportTicketDescription = ticket.Description,
            StaffId = staffId,
            DateActioned = DateTime.UtcNow
        }, ticket);
    }

    public Task<List<SupportTicket>> GetOpenSupportTicketsAsync() => _repository.GetSupportTicketsAsync("Open");

    public Task<List<SupportTicket>> GetAllSupportTicketsAsync() => _repository.GetSupportTicketsAsync();

    public Task<SupportTicket?> GetSupportTicketAsync(int ticketId)
    {
        ValidateId(ticketId, nameof(ticketId));
        return _repository.GetSupportTicketByIdAsync(ticketId);
    }

    public Task<bool> ResolveSupportTicketAsync(int ticketId, int staffId, string resolutionNotes)
    {
        ValidateId(ticketId, nameof(ticketId));
        ValidateStaffId(staffId);

        if (string.IsNullOrWhiteSpace(resolutionNotes))
        {
            throw new ArgumentException("Resolution notes are required.", nameof(resolutionNotes));
        }

        return _repository.ResolveSupportTicketAsync(ticketId, staffId, resolutionNotes.Trim());
    }

    private static void ValidateCommand(int returnTaskId, int staffId)
    {
        ValidateId(returnTaskId, nameof(returnTaskId));
        ValidateStaffId(staffId);
    }

    private static void ValidateStaffId(int staffId) => ValidatePositiveId(staffId, nameof(staffId));

    private static void ValidateId(int value, string parameterName) => ValidatePositiveId(value, parameterName);

    private static void ValidatePositiveId(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentException("The identifier must be greater than zero.", parameterName);
        }
    }
}
