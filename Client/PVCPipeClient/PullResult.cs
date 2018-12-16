namespace PVCPipeLibrary
{
    public partial class PVCServerInterface
    {
        public enum PullResult
        {
            Success = 0,
            Uncommitted_Changes,
            Merge_Conflict
        }
    }
}
