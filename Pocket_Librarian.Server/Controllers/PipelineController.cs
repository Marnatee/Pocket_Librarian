using Microsoft.AspNetCore.Mvc;
using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Services;

namespace Pocket_Librarian.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PipelineController : ControllerBase
    {
        private readonly PipelineService _pipelineService;

        public PipelineController(PipelineService pipelineService)
        {
            _pipelineService = pipelineService;
        }

        [HttpGet("test")]
        public ActionResult<PipelineResponse> TestPipeline()
        {
            PipelineResponse response = _pipelineService.RunPipelineTest();

            response.Steps.Insert(
                0,
                "API controller received the HTTP request."
                );

            response.Steps.Add(
                "API controller is sending the JSON response."
                );

            return Ok(response);
        }
    }
}
