using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Pocket_Librarian.BLL.Models;
using Pocket_Librarian.BLL.Services;

namespace Pocket_Librarian.Server.Controllers
{
    // Provides HTTP endpoints for public catalogue operations.
    // Requests are delegated to the BLL rather than accessing the database directly.
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogueController : ControllerBase
    {
        private readonly CatalogueService _service;

        // Receives the catalogue service used to handle catalogue requests.
        public CatalogueController(CatalogueService service)
        {
            _service = service;
        }

        // Returns all genres available for catalogue filtering.
        [HttpGet("genres")]
        public ActionResult<List<CatalogueGenre>> GetGenres()
        {
            try
            {
                List<CatalogueGenre> genres = _service.GetGenres();
                return Ok(genres);
            }
            catch (SqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { message = "The catalogue service could not reach the database." });
            }
        }
    }
}