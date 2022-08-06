namespace Speedbump
{
    [Serializable]
    public class GuildConfig
    {
        public string Item { get; set; }
        public string Value { get; set; }
        public string Default { get; set; }
        public string Label { get; set; }
        public GuildConfigType Type { get; set; }
    }

    public enum GuildConfigType
    {
        Role = 0,
        TextChannel = 1,
        Category = 2,
        Text = 3,
    }
}
