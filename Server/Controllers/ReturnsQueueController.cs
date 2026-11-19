// API entry point
// Receives the HTTP request from React and sends the HTTP/JSON
// response back.

using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.BLL.Services;

namespace Pocket_Librarian.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReturnsQueueController : ControllerBase
    {
        private readonly ReturnsQueueService _returnsQueueService;

        public ReturnsQueueController(ReturnsQueueService returnsQueueService)
        {
            _returnsQueueService = returnsQueueService;
        }

        // Get all pending return tasks for the current staff member.
        [HttpGet("my-tasks/{staffId}")]
        public async Task<ActionResult<List<ReturnTask>>> GetMyPendingTasks(int staffId)
        {
            try
            {
                var tasks = await _returnsQueueService.GetMyPendingTasksAsync(staffId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving tasks: {ex.Message}" });
            }
        }

        // Get all pending return tasks (admin/manager view).
        [HttpGet("all-pending")]
        public async Task<ActionResult<List<ReturnTask>>> GetAllPendingTasks()
        {
            try
            {
                var tasks = await _returnsQueueService.GetAllPendingTasksAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving tasks: {ex.Message}" });
            }
        }

        // Get a specific return task by ID.
        [HttpGet("task/{returnTaskId}")]
        public async Task<ActionResult<ReturnTask>> GetReturnTask(int returnTaskId)
        {
            try
            {
                var task = await _returnsQueueService.GetReturnTaskAsync(returnTaskId);
                if (task == null)
                {
                    return NotFound(new { message = "Return task not found." });
                }
                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving task: {ex.Message}" });
            }
        }

        // Handle the "Complete" action for a returned book.
        // Marks the task as completed and assigns the next pending task.
        [HttpPost("action/complete")]
        public async Task<ActionResult<ReturnActionResponse>> CompleteAction([FromBody] CompleteActionRequest request)
        {
            try
            {
                var response = await _returnsQueueService.HandleCompleteActionAsync(
                    request.ReturnTaskId,
                    request.StaffId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReturnActionResponse
                {
                    Success = false,
                    Message = $"Error processing action: {ex.Message}"
                });
            }
        }

        // Handle the "Another Book Was in This Location" action.
        // Completes the current task and creates a new task for the found book.
        [HttpPost("action/another-book")]
        public async Task<ActionResult<ReturnActionResponse>> AnotherBookAction(
            [FromBody] AnotherBookActionRequest request)
        {
            try
            {
                var response = await _returnsQueueService.HandleAnotherBookActionAsync(
                    request.ReturnTaskId,
                    request.StaffId,
                    request.FoundBookTitle,
                    request.FoundBookShelfLocation);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReturnActionResponse
                {
                    Success = false,
                    Message = $"Error processing action: {ex.Message}"
                });
            }
        }

        // Handle the "Bad Condition" action.
        // Completes the task and flags the book for deletion.
        [HttpPost("action/bad-condition")]
        public async Task<ActionResult<ReturnActionResponse>> BadConditionAction(
            [FromBody] BadConditionActionRequest request)
        {
            try
            {
                var response = await _returnsQueueService.HandleBadConditionActionAsync(
                    request.ReturnTaskId,
                    request.StaffId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReturnActionResponse
                {
                    Success = false,
                    Message = $"Error processing action: {ex.Message}"
                });
            }
        }

        // Handle the "Other" action.
        // Completes the task and creates a support ticket.
        [HttpPost("action/other")]
        public async Task<ActionResult<ReturnActionResponse>> OtherAction(
            [FromBody] OtherActionRequest request)
        {
            try
            {
                var response = await _returnsQueueService.HandleOtherActionAsync(
                    request.ReturnTaskId,
                    request.StaffId,
                    request.TicketDescription);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReturnActionResponse
                {
                    Success = false,
                    Message = $"Error processing action: {ex.Message}"
                });
            }
        }

        // Get all open support tickets.
        [HttpGet("support-tickets/open")]
        public async Task<ActionResult<List<SupportTicket>>> GetOpenSupportTickets()
        {
            try
            {
                var tickets = await _returnsQueueService.GetOpenSupportTicketsAsync();
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving tickets: {ex.Message}" });
            }
        }

        /// Get all support tickets.
        [HttpGet("support-tickets")]
        public async Task<ActionResult<List<SupportTicket>>> GetAllSupportTickets()
        {
            try
            {
                var tickets = await _returnsQueueService.GetAllSupportTicketsAsync();
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving tickets: {ex.Message}" });
            }
        }

        // Get a specific support ticket.
        [HttpGet("support-tickets/{ticketId}")]
        public async Task<ActionResult<SupportTicket>> GetSupportTicket(int ticketId)
        {
            try
            {
                var ticket = await _returnsQueueService.GetSupportTicketAsync(ticketId);
                if (ticket == null)
                {
                    return NotFound(new { message = "Support ticket not found." });
                }
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving ticket: {ex.Message}" });
            }
        }
    }

    // Request DTOs for API endpoints
    public class CompleteActionRequest
    {
        public int ReturnTaskId { get; set; }
        public int StaffId { get; set; }
    }

    public class AnotherBookActionRequest
    {
        public int ReturnTaskId { get; set; }
        public int StaffId { get; set; }
        public string FoundBookTitle { get; set; } = string.Empty;
        public string FoundBookShelfLocation { get; set; } = string.Empty;
    }

    public class BadConditionActionRequest
    {
        public int ReturnTaskId { get; set; }
        public int StaffId { get; set; }
    }

    public class OtherActionRequest
    {
        public int ReturnTaskId { get; set; }
        public int StaffId { get; set; }
        public string TicketDescription { get; set; } = string.Empty;
    }
}
