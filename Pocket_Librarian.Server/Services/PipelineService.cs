// Business Logic Layer.
// Controls what the application should do and calls the DAL
// when data access is needed.

using Pocket_Librarian.Server.Models;
using Pocket_Librarian.Server.Repositories;

namespace Pocket_Librarian.Server.Services
{
    public class PipelineService
    {
        private readonly PipelineRepository _repository;

        public PipelineService(PipelineRepository repository)
        {
            _repository = repository;
        }

        public PipelineResponse RunPipelineTest ()
        {
            PipelineResponse response = new PipelineResponse();

            response.Steps.Add(
                "Business layer received the request."
                );

            response.Steps.Add(
                "Business layer is calling the data-access layer."
                );

            string repositoryMessage =
                _repository.TestDataAccess();

            response.Steps.Add(repositoryMessage);

            response.Steps.Add(
                "Data-access result returned to the business layer"
                );

            response.Success = true;
            response.FinalMessage =
                "The complete three-layer pipeline worked.";

            return response;
        }
    }
}
