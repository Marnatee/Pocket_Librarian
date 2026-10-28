// Backend-only health endpoint.
// This endpoint intentionally does not call the business or data-access layers.

using Microsoft.AspNetCore.Mvc;

namespace Pocket_Librarian.Server.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                Success = true,
                BackendConnected = true,
                Message = "The local ASP.NET Core backend is responding."
            });
        }
    }
}
