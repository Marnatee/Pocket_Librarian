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
            var repositoryResult =
                _repository.TestDataAccess();

            return BuildPipelineResponse(repositoryResult);
        }

        public PipelineResponse ConnectManualDatabase(string connectionString)
        {
            var repositoryResult =
                _repository.TestManualDataAccess(connectionString);

            return BuildPipelineResponse(repositoryResult);
        }

        private static PipelineResponse BuildPipelineResponse(
            (bool Connected, string Message, string Details) repositoryResult)
        {
            PipelineResponse response = new PipelineResponse();

            response.Steps.Add(
                "Business layer received the request."
                );

            response.Steps.Add(
                "Business layer is calling the data-access layer."
                );

            response.DatabaseConnected = repositoryResult.Connected;
            response.DatabaseMessage = repositoryResult.Message;
            response.DatabaseDetails = repositoryResult.Details;

            response.Steps.Add(repositoryResult.Message);

            response.Steps.Add(
                "Data-access result returned to the business layer"
                );

            response.Success = true;
            response.FinalMessage =
                repositoryResult.Connected
                    ? "The complete three-layer pipeline worked."
                    : "The API pipeline responded, but the database is unavailable.";

            return response;
        }
    }
}
