// Business Logic Layer.
// Controls what the application should do and calls the DAL
// when data access is needed.

using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Models;

namespace Pocket_Librarian.BLL.Services
{
    public class ReturnsQueueService
    {
        private readonly IReturnsQueueRepository _repository;
        private readonly IReservationRepository _reservationRepository;

        public ReturnsQueueService(
            IReturnsQueueRepository repository,
            IReservationRepository reservationRepository)
        {
            _repository = repository;
            _reservationRepository = reservationRepository;
        }

        // Get all pending return tasks for a specific staff member.
        public async Task<List<ReturnTask>> GetMyPendingTasksAsync(int staffId)
        {
            return await _repository.GetPendingTasksForStaffAsync(staffId);
        }

        // Get all pending return tasks (admin/manager view).
        public async Task<List<ReturnTask>> GetAllPendingTasksAsync()
        {
            return await _repository.GetAllPendingTasksAsync();
        }

        // Get a specific return task.

        public async Task<ReturnTask?> GetReturnTaskAsync(int returnTaskId)
        {
            return await _repository.GetReturnTaskByIdAsync(returnTaskId);
        }


        // Create a new return task when a book is returned.
        // Typically called from the reservation return workflow.

        public async Task<int> CreateReturnTaskAsync(int reservationId, int bookId, string bookTitle, string shelfLocation)
        {
            var task = new ReturnTask
            {
                ReservationId = reservationId,
                BookId = bookId,
                BookTitle = bookTitle,
                ShelfLocation = shelfLocation,
                Status = "Pending",
                DateCreated = DateTime.UtcNow
            };

            return await _repository.CreateReturnTaskAsync(task);
        }

        // Process a returned book with the "Complete" action.
        // Marks the task as completed and assigns the next pending task if available.
        public async Task<ReturnActionResponse> HandleCompleteActionAsync(
            int returnTaskId,
            int staffId)
        {
            var response = new ReturnActionResponse { Success = false };

            try
            {
                // Complete the current task
                bool taskCompleted = await _repository.CompleteReturnTaskAsync(returnTaskId);
                if (!taskCompleted)
                {
                    response.Message = "Failed to complete the return task.";
                    return response;
                }

                // Record the action
                var action = new ReturnAction
                {
                    ActionType = "Complete",
                    DateActioned = DateTime.UtcNow,
                    StaffId = staffId
                };
                await _repository.RecordReturnActionAsync(action, returnTaskId);

                // Get the completed task for response
                var completedTask = await _repository.GetReturnTaskByIdAsync(returnTaskId);

                // Assign the next pending task to this staff member
                var pendingTasks = await _repository.GetAllPendingTasksAsync();
                ReturnTask? nextTask = null;

                if (pendingTasks.Count > 0)
                {
                    nextTask = pendingTasks[0]; // Get the first pending task
                    nextTask.AssignedStaffId = staffId;
                    await _repository.UpdateReturnTaskAsync(nextTask);
                }

                response.Success = true;
                response.Message = "Task completed successfully. " + (nextTask != null ? "Next task assigned." : "No more pending tasks.");
                response.CompletedTask = completedTask;
                response.NewTask = nextTask;

                return response;
            }
            catch (Exception ex)
            {
                response.Message = $"Error processing complete action: {ex.Message}";
                return response;
            }
        }

        // Process a returned book with the "Another Book Was in This Location" action.
        // Completes the current task and creates a new task for the found book.
        public async Task<ReturnActionResponse> HandleAnotherBookActionAsync(
            int returnTaskId,
            int staffId,
            string foundBookTitle,
            string foundBookShelfLocation)
        {
            var response = new ReturnActionResponse { Success = false };

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(foundBookTitle) || string.IsNullOrWhiteSpace(foundBookShelfLocation))
                {
                    response.Message = "Book title and shelf location are required.";
                    return response;
                }

                // Complete the current task
                bool taskCompleted = await _repository.CompleteReturnTaskAsync(returnTaskId);
                if (!taskCompleted)
                {
                    response.Message = "Failed to complete the return task.";
                    return response;
                }

                // Record the action with found book details
                var action = new ReturnAction
                {
                    ActionType = "AnotherBook",
                    FoundBookTitle = foundBookTitle,
                    FoundBookShelfLocation = foundBookShelfLocation,
                    DateActioned = DateTime.UtcNow,
                    StaffId = staffId
                };
                await _repository.RecordReturnActionAsync(action, returnTaskId);

                // Get the completed task for response
                var completedTask = await _repository.GetReturnTaskByIdAsync(returnTaskId);

                // Create a new task for the found book
                // Note: BookId is unknown for a newly found book, so we use 0 as placeholder
                // The system should require staff to identify the book later
                var newTask = new ReturnTask
                {
                    ReservationId = 0, // No reservation for a found book
                    BookId = 0, // BookId unknown until staff identifies the book
                    BookTitle = foundBookTitle,
                    ShelfLocation = foundBookShelfLocation,
                    AssignedStaffId = staffId,
                    Status = "Pending",
                    DateCreated = DateTime.UtcNow
                };

                int newTaskId = await _repository.CreateReturnTaskAsync(newTask);
                var createdTask = await _repository.GetReturnTaskByIdAsync(newTaskId);

                response.Success = true;
                response.Message = $"Task completed. New task created for '{foundBookTitle}'.";
                response.CompletedTask = completedTask;
                response.NewTask = createdTask;

                return response;
            }
            catch (Exception ex)
            {
                response.Message = $"Error processing 'another book' action: {ex.Message}";
                return response;
            }
        }

        // Process a returned book with the "Bad Condition" action.
        // Completes the task, flags the book for deletion, and marks it as unavailable.
        public async Task<ReturnActionResponse> HandleBadConditionActionAsync(
            int returnTaskId,
            int staffId)
        {
            var response = new ReturnActionResponse { Success = false };

            try
            {
                // Get the task to access the book ID
                var task = await _repository.GetReturnTaskByIdAsync(returnTaskId);
                if (task == null)
                {
                    response.Message = "Return task not found.";
                    return response;
                }

                // Complete the current task
                bool taskCompleted = await _repository.CompleteReturnTaskAsync(returnTaskId);
                if (!taskCompleted)
                {
                    response.Message = "Failed to complete the return task.";
                    return response;
                }

                // Record the action
                var action = new ReturnAction
                {
                    ActionType = "BadCondition",
                    DateActioned = DateTime.UtcNow,
                    StaffId = staffId
                };
                await _repository.RecordReturnActionAsync(action, returnTaskId);

                // Flag the book for deletion
                bool flagged = await _repository.FlagBookForDeletionAsync(task.BookId, "Book returned in bad condition");
                if (!flagged)
                {
                    response.Message = "Task completed but failed to flag book for deletion.";
                    return response;
                }

                var completedTask = await _repository.GetReturnTaskByIdAsync(returnTaskId);

                response.Success = true;
                response.Message = "Task completed. Book flagged for deletion.";
                response.CompletedTask = completedTask;

                return response;
            }
            catch (Exception ex)
            {
                response.Message = $"Error processing 'bad condition' action: {ex.Message}";
                return response;
            }
        }

        // Process a returned book with the "Other" action.
        // Completes the task, creates a support ticket, and sets the book aside.
        public async Task<ReturnActionResponse> HandleOtherActionAsync(
            int returnTaskId,
            int staffId,
            string ticketDescription)
        {
            var response = new ReturnActionResponse { Success = false };

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(ticketDescription))
                {
                    response.Message = "Support ticket description is required.";
                    return response;
                }

                // Get the task to access the book ID
                var task = await _repository.GetReturnTaskByIdAsync(returnTaskId);
                if (task == null)
                {
                    response.Message = "Return task not found.";
                    return response;
                }

                // Complete the current task
                bool taskCompleted = await _repository.CompleteReturnTaskAsync(returnTaskId);
                if (!taskCompleted)
                {
                    response.Message = "Failed to complete the return task.";
                    return response;
                }

                // Record the action with ticket description
                var action = new ReturnAction
                {
                    ActionType = "Other",
                    SupportTicketDescription = ticketDescription,
                    DateActioned = DateTime.UtcNow,
                    StaffId = staffId
                };
                await _repository.RecordReturnActionAsync(action, returnTaskId);

                // Create a support ticket
                var ticket = new SupportTicket
                {
                    ReturnTaskId = returnTaskId,
                    BookId = task.BookId,
                    CreatedByStaffId = staffId,
                    Description = ticketDescription,
                    Status = "Open",
                    DateCreated = DateTime.UtcNow
                };

                int ticketId = await _repository.CreateSupportTicketAsync(ticket);
                var createdTicket = await _repository.GetSupportTicketByIdAsync(ticketId);

                var completedTask = await _repository.GetReturnTaskByIdAsync(returnTaskId);

                response.Success = true;
                response.Message = "Task completed. Support ticket created.";
                response.CompletedTask = completedTask;
                response.SupportTicket = createdTicket;

                return response;
            }
            catch (Exception ex)
            {
                response.Message = $"Error processing 'other' action: {ex.Message}";
                return response;
            }
        }

        /// Get all open support tickets.
        public async Task<List<SupportTicket>> GetOpenSupportTicketsAsync()
        {
            return await _repository.GetSupportTicketsAsync("Open");
        }

        // Get all support tickets.
        public async Task<List<SupportTicket>> GetAllSupportTicketsAsync()
        {
            return await _repository.GetSupportTicketsAsync();
        }

        // Get a specific support ticket.
        public async Task<SupportTicket?> GetSupportTicketAsync(int ticketId)
        {
            return await _repository.GetSupportTicketByIdAsync(ticketId);
        }
    }
}
