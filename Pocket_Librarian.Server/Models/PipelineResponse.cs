// Data-transfermodel
// Defines the response informaton that travels
// back to React as JSON

namespace Pocket_Librarian.Server.Models
{
    public class PipelineResponse
    {
        public bool Success { get; set; }

        public string FinalMessage { get; set; } = "";

        public List<string> Steps { get; set; } = new();
    }
}
