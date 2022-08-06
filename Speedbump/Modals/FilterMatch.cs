using DSharpPlus.SlashCommands;

namespace Speedbump
{
    [Serializable]
    public class FilterMatch
    {
        public ulong Guild { get; set; }
        public string Match { get; set; }
        public FilterMatchType Type { get; set; }
    }

    public enum FilterMatchType
    {
        [ChoiceName("None")]
        None = 2,
        [ChoiceName("Flag Only")]
        Flag = 0,
        [ChoiceName("Auto Mute")]
        Mute = 1,
    }
}
