using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;

namespace Pocket_Librarian.Server.Controllers;

// Identifies this class as an API controller
[ApiController]

// Sets the base URL for profile requests
[Route("api/member-profile")]
public class MemberProfileController : ControllerBase
{
    // Provides access to profile database operations
    private readonly MemberProfileRepository _repository;

    // Receives the repository through dependency injection
    public MemberProfileController(
        MemberProfileRepository repository)
    {
        _repository = repository;
    }

    // Retrieves a member profile
    [HttpGet("{username}")]
    public async Task<ActionResult<MemberProfile>>
        GetProfile(string username)
    {
        // Requests the profile from the repository
        MemberProfile? profile =
            await _repository.GetByUsernameAsync(
                username);

        // Returns a 404 response if no profile was found
        if (profile == null)
        {
            return NotFound(
                "User profile was not found.");
        }

        // Returns the profile as JSON
        return Ok(profile);
    }
}