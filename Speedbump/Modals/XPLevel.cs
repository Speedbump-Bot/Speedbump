using System.ComponentModel.DataAnnotations.Schema;

namespace Speedbump
{
    [Table("XPLevel")]
    public class XPLevel
    {
        [Column("Guild", TypeName = "bigint")]
        public ulong Guild { get; set; }
        [Column("XPLevel")]
        public int Level { get; set; }
        public ulong Role { get; set; }
    }
}
