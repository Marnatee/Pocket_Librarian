using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;

namespace Pocket_Librarian.Server.Controllers;

// Identifies this class as an API controller
[ApiController]

// Sets the base URL for message requests
[Route("api/messages")]
public class MessageController : ControllerBase
{
    private readonly MessageRepository _repository;

    // Receives the repository through dependency injection
    public MessageController(
        MessageRepository repository)
    {
        _repository = repository;
    }

    // Retrieves a member's inbox
    //
    // GET /api/messages/inbox/GregBooks
    [HttpGet("inbox/{username}")]
    public async Task<ActionResult<List<Message>>>
        GetInbox(string username)
    {
        // Retrieves the messages from the repository
        List<Message> messages =
            await _repository.GetInboxAsync(
                username);

        // Returns the messages as JSON
        return Ok(messages);
    }

    // Sends a message to another member
    //
    // POST /api/messages/send/LibraryStaff/GregBooks
    [HttpPost("send/{senderUsername}/{recipientUsername}")]
    public async Task<IActionResult> SendMessage(
        string senderUsername,
        string recipientUsername,
        [FromBody]
        CreateMessageRequest request)
    {
        // Saves the message and creates a notification
        bool sent =
            await _repository.SendMessageAsync(
                senderUsername,
                recipientUsername,
                request);

        if (!sent)
        {
            return NotFound(
                "The sender or recipient was not found.");
        }

        return Ok(
            "Message sent and notification created.");
    }
}