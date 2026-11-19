using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;

namespace Pocket_Librarian.Server.Controllers;

// Identifies this class as an API controller
[ApiController]

// Sets the base URL for preference requests
[Route("api/preferences")]
public class UserPreferencesController : ControllerBase
{
    // Provides access to preference database operations
    private readonly UserPreferencesRepository _repository;

    // Receives the repository through dependency injection
    public UserPreferencesController(
        UserPreferencesRepository repository)
    {
        _repository = repository;
    }

    // Retrieves a member's preferences
    //
    // Example:
    // GET /api/preferences/GregBooks
    [HttpGet("{username}")]
    public async Task<ActionResult<UserPreferences>>
        GetPreferences(string username)
    {
        // Requests the preferences from the repository
        UserPreferences? preferences =
            await _repository.GetPreferencesAsync(
                username);

        // Returns 404 if the username does not exist
        if (preferences == null)
        {
            return NotFound(
                "User was not found.");
        }

        // Returns the preferences as JSON
        return Ok(preferences);
    }

    // Updates a member's preferences
    [HttpPut("{username}")]
    public async Task<IActionResult> UpdatePreferences(
        string username,
        [FromBody]
        UpdateUserPreferencesRequest preferences)
    {
        // Saves the new preferences
        bool updated =
            await _repository.UpdatePreferencesAsync(
                username,
                preferences);

        // Returns 404 if the username does not exist
        if (!updated)
        {
            return NotFound(
                "User was not found.");
        }

        // Confirms that the update was successful
        return Ok(
            "Preferences updated successfully.");
    }
}