namespace PVCPipeLibrary
{
    public partial class PVCServerInterface
    {
        public enum PushResult
        {
            Success = 0,
            Uncommited_Changes,
            Conflict,
            Authentication_Error,
            Bad_Request,
            No_Response_From_Server,
            Up_to_Date
        }
    }
}
