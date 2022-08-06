namespace Speedbump
{
    public class FlagHistory
    {
        public ulong Flag { get; set; }
        public FlagResolutionType Type { get; set; }
        public DateTime Time { get; set; }
        public ulong User { get; set; }
    }
}
