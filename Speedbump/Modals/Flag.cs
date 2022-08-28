namespace Speedbump
{
    [Serializable]
    public class Flag
    {
        public ulong ID { get; set; }
        public ulong Guild { get; set; }
        public ulong? Message { get; set; }
        public DateTime Time { get; set; }
        public FlagType Type { get; set; }
        public ulong? FlaggedBy { get; set; }
        public ulong SourceMessage { get; set; }
        public ulong SourceUser { get; set; }
        public string SourceContent { get; set; }
        public string SourceMatches { get; set; }
        public ulong SourceChannel { get; set; }
        public ulong SourceGuild { get; set; }

        public ulong? ResolutionUser { get; set; }
        public DateTime ResolutionTime { get; set; }
        public FlagResolutionType ResolutionType { get; set; }
        public int ResolutionPoints { get; set; }
        public string SystemMessage { get; set; }
    }
    
    public enum FlagType
    {
        Message = 0,
        User = 1,
    }

    public enum FlagResolutionType
    {
        None = 0,
        Cleared = 1,
        Warned = 2,
        Muted = 3,
    }
}
