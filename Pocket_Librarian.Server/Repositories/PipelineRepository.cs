namespace Pocket_Librarian.Server.Repositories
{
    public class PipelineRepository
    {
        public string TestDataAccess()
        {
            //A real repository would query the SQLdatabase here.
            //For now, it simply acknowledges that it was reached.
            return "Data-access layer received the request successfully.";
            //Replace this line later with Entity Framework or SQL operation
        }
    }
}
