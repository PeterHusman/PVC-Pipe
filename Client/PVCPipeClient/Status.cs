namespace PVCPipeLibrary
{
    public partial class PVCServerInterface
    {
        public enum Status
        {
            Uncommitted_Changes,
            Ahead_of_Origin,
            Behind_Origin,
            Up_to_Date
        }
    }
}
