// API controller for staff returns-queue operations.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.BLL.Services;

namespace Pocket_Librarian.Server.Controllers;

[ApiController]
[Route("api/returnsqueue")]
[Authorize(Roles = "Staff")]
public class ReturnsQueueController : ControllerBase
{
    private readonly ReturnsQueueService _service;

    public ReturnsQueueController(ReturnsQueueService service)
    {
        _service = service;
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyPendingTasks()
    {
        return await ExecuteAsync(
            () => _service.GetMyPendingTasksAsync(GetCurrentStaffId()),
            tasks => Ok(tasks));
    }

    [HttpGet("all-pending")]
    public async Task<IActionResult> GetAllPendingTasks()
    {
        return await ExecuteAsync(_service.GetAllPendingTasksAsync, tasks => Ok(tasks));
    }

    [HttpGet("task/{returnTaskId:int}")]
    public async Task<IActionResult> GetReturnTask(int returnTaskId)
    {
        return await ExecuteAsync(
            () => _service.GetReturnTaskAsync(returnTaskId),
            task => task is null ? NotFound(new { message = "Return task not found." }) : Ok(task));
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateReturnTask([FromBody] CreateReturnTaskRequest request)
    {
        return await ExecuteAsync(
            async () => new
            {
                returnTaskId = await _service.CreateReturnTaskAsync(
                    request.ReservationId,
                    request.BookId,
                    request.BookTitle,
                    request.ShelfLocation)
            },
            result => Created("api/returnsqueue/task/" + result.returnTaskId, result));
    }

    [HttpPost("claim-next")]
    public async Task<IActionResult> ClaimNextTask()
    {
        return await ExecuteAsync(
            () => _service.ClaimNextTaskAsync(GetCurrentStaffId()),
            task => task is null ? NoContent() : Ok(task));
    }

    [HttpPost("claim/{returnTaskId:int}")]
    public async Task<IActionResult> ClaimTask(int returnTaskId)
    {
        return await ExecuteAsync(
            () => _service.ClaimTaskAsync(returnTaskId, GetCurrentStaffId()),
            task => task is null
                ? Conflict(new { message = "The task is unavailable or already assigned." })
                : Ok(task));
    }

    [HttpPost("action/complete")]
    public async Task<IActionResult> CompleteAction([FromBody] TaskActionRequest request)
    {
        return await ExecuteAsync(
            () => _service.HandleCompleteActionAsync(request.ReturnTaskId, GetCurrentStaffId()),
            response => response.Success ? Ok(response) : Conflict(response));
    }

    [HttpPost("action/another-book")]
    public async Task<IActionResult> AnotherBookAction(
        [FromBody] AnotherBookActionRequest request)
    {
        return await ExecuteAsync(
            () => _service.HandleAnotherBookActionAsync(
                request.ReturnTaskId,
                GetCurrentStaffId(),
                request.FoundBookId,
                request.FoundBookTitle,
                request.FoundBookShelfLocation),
            response => response.Success ? Ok(response) : Conflict(response));
    }

    [HttpPost("action/bad-condition")]
    public async Task<IActionResult> BadConditionAction(
        [FromBody] TaskActionRequest request)
    {
        return await ExecuteAsync(
            () => _service.HandleBadConditionActionAsync(request.ReturnTaskId, GetCurrentStaffId()),
            response => response.Success ? Ok(response) : Conflict(response));
    }

    [HttpPost("action/other")]
    public async Task<IActionResult> OtherAction(
        [FromBody] OtherActionRequest request)
    {
        return await ExecuteAsync(
            () => _service.HandleOtherActionAsync(
                request.ReturnTaskId,
                GetCurrentStaffId(),
                request.TicketDescription),
            response => response.Success ? Ok(response) : Conflict(response));
    }

    [HttpGet("support-tickets/open")]
    public async Task<IActionResult> GetOpenSupportTickets()
    {
        return await ExecuteAsync(_service.GetOpenSupportTicketsAsync, tickets => Ok(tickets));
    }

    [HttpGet("support-tickets")]
    public async Task<IActionResult> GetAllSupportTickets()
    {
        return await ExecuteAsync(_service.GetAllSupportTicketsAsync, tickets => Ok(tickets));
    }

    [HttpGet("support-tickets/{ticketId:int}")]
    public async Task<IActionResult> GetSupportTicket(int ticketId)
    {
        return await ExecuteAsync(
            () => _service.GetSupportTicketAsync(ticketId),
            ticket => ticket is null ? NotFound(new { message = "Support ticket not found." }) : Ok(ticket));
    }

    [HttpPost("support-tickets/{ticketId:int}/resolve")]
    public async Task<IActionResult> ResolveSupportTicket(
        int ticketId,
        [FromBody] ResolveSupportTicketRequest request)
    {
        return await ExecuteAsync(
            async () => await _service.ResolveSupportTicketAsync(
                ticketId,
                GetCurrentStaffId(),
                request.ResolutionNotes),
            resolved => resolved
                ? Ok(new { message = "Support ticket resolved." })
                : NotFound(new { message = "Open support ticket not found." }));
    }

    private int GetCurrentStaffId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out int staffId) || staffId <= 0)
        {
            throw new UnauthorizedAccessException("The signed-in staff member could not be identified.");
        }

        return staffId;
    }

    private static async Task<IActionResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        Func<TResult, IActionResult> responseFactory)
    {
        try
        {
            return responseFactory(await operation());
        }
        catch (ArgumentException ex)
        {
            return new BadRequestObjectResult(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return new UnauthorizedObjectResult(new { message = ex.Message });
        }
        catch (Exception)
        {
            return new ObjectResult(new { message = "The returns-queue operation failed." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}

public sealed class CreateReturnTaskRequest
{
    public int? ReservationId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string ShelfLocation { get; set; } = "Unassigned";
}

public class TaskActionRequest
{
    public int ReturnTaskId { get; set; }
}

public sealed class AnotherBookActionRequest : TaskActionRequest
{
    public int FoundBookId { get; set; }
    public string FoundBookTitle { get; set; } = string.Empty;
    public string FoundBookShelfLocation { get; set; } = string.Empty;
}

public sealed class OtherActionRequest : TaskActionRequest
{
    public string TicketDescription { get; set; } = string.Empty;
}

public sealed class ResolveSupportTicketRequest
{
    public string ResolutionNotes { get; set; } = string.Empty;
}
