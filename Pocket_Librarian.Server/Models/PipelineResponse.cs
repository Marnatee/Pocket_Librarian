// Data-transfermodel
// Defines the response informaton that travels
// back to React as JSON

namespace Pocket_Librarian.Server.Models
{
    public class PipelineResponse
    {
        public bool Success { get; set; }

        public bool DatabaseConnected { get; set; }

        public string DatabaseMessage { get; set; } = "";

        public string DatabaseDetails { get; set; } = "";

        public string FinalMessage { get; set; } = "";

        public List<string> Steps { get; set; } = new();
    }
}
