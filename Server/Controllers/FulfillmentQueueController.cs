// API controller for staff fulfillment queue operations.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.BLL.Services;

namespace Pocket_Librarian.Server.Controllers;

[ApiController]
[Route("api/fulfillmentqueue")]
[Authorize(Roles = "Staff")]
public class FulfillmentQueueController : ControllerBase
{
    private readonly FulfillmentQueueService _service;

    public FulfillmentQueueController(FulfillmentQueueService service)
    {
        _service = service;
    }

    [HttpGet("all-pending")]
    public async Task<IActionResult> GetAllPendingTasks()
    {
        return await ExecuteAsync(_service.GetAllPendingTasksAsync, tasks => Ok(tasks));
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyPendingTasks()
    {
        return await ExecuteAsync(
            () => _service.GetMyPendingTasksAsync(GetCurrentStaffId()),
            tasks => Ok(tasks));
    }

    [HttpGet("task/{fulfillmentTaskId:int}")]
    public async Task<IActionResult> GetFulfillmentTask(int fulfillmentTaskId)
    {
        return await ExecuteAsync(
            () => _service.GetFulfillmentTaskAsync(fulfillmentTaskId),
            task => task is null ? NotFound(new { message = "Fulfillment task not found." }) : Ok(task));
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateFulfillmentTask([FromBody] CreateFulfillmentTaskRequest request)
    {
        return await ExecuteAsync(
            async () => new
            {
                fulfillmentTaskId = await _service.CreateFulfillmentTaskAsync(
                    request.ReservationId,
                    request.UserId,
                    request.BookId,
                    request.BookTitle,
                    request.MemberName)
            },
            result => Created("api/fulfillmentqueue/task/" + result.fulfillmentTaskId, result));
    }

    [HttpPost("claim-next")]
    public async Task<IActionResult> ClaimNextTask()
    {
        return await ExecuteAsync(
            () => _service.ClaimNextTaskAsync(GetCurrentStaffId()),
            task => task is null ? NoContent() : Ok(task));
    }

    [HttpPost("claim/{fulfillmentTaskId:int}")]
    public async Task<IActionResult> ClaimTask(int fulfillmentTaskId)
    {
        return await ExecuteAsync(
            () => _service.ClaimTaskAsync(fulfillmentTaskId, GetCurrentStaffId()),
            task => task is null
                ? Conflict(new { message = "The task is unavailable or already assigned." })
                : Ok(task));
    }

    [HttpPost("fulfill/{fulfillmentTaskId:int}")]
    public async Task<IActionResult> FulfillTask(int fulfillmentTaskId)
    {
        return await ExecuteAsync(
            () => _service.MarkTaskFulfilledAsync(fulfillmentTaskId, GetCurrentStaffId()),
            task => task is null
                ? Conflict(new { message = "The task could not be marked as fulfilled." })
                : Ok(task));
    }

    [HttpGet("completed")]
    public async Task<IActionResult> GetCompletedTasks()
    {
        return await ExecuteAsync(_service.GetCompletedTasksAsync, tasks => Ok(tasks));
    }

    //Helper method to extract the current staff ID from the authenticated user's claims.

    private int GetCurrentStaffId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim?.Value, out int staffId))
        {
            return staffId;
        }
        throw new UnauthorizedAccessException("Staff ID not found in authentication claims.");
    }

    private async Task<IActionResult> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Func<T, IActionResult> successHandler)
    {
        try
        {
            T result = await operation();
            return successHandler(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while processing the fulfillment queue request.", details = ex.Message });
        }
    }
}

//Request DTO 

public class CreateFulfillmentTaskRequest
{
    public int ReservationId { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
}
 