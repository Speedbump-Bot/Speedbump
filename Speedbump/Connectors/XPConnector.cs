namespace Speedbump
{
    public static class XPConnector
    {
        public static void Increment(ulong guild, ulong user) =>
            new SqlInstance().Execute(@"insert into @p0xp values (@p1,@p2,1)
	                                        on duplicate key update xp=xp+1", guild, user);

        public static int GetXP(ulong guild, ulong user) =>
            (int)Math.Floor((decimal)new SqlInstance().Read(@"select coalesce(sum(xp), 0) from @p0xp where guild=@p1 and user=@p2", guild, user).Rows[0][0]);

        public static int GetLevel(ulong guild, ulong user) =>
            (int)Math.Floor((1 + Math.Sqrt(1 + (8 * GetXP(guild, user)) / (double)200)) / 2);

        public static int GetMinXP(int level) =>
            (int)Math.Round(200 * (Math.Pow(2 * level - 1, 2) - 1) / 8);

        public static List<(int rank, ulong user, int xp)> Leaderboard(ulong guild)
        {
            var sql = new SqlInstance();
            var all = sql.Read("select * from @p0xp where guild=@p1 order by xp desc", guild);
            var toReturn = new List<(int, ulong, int)>();
            for (var i = 0; i < all.RowCount; i++)
            {
                toReturn.Add((i + 1, ulong.Parse(all.Rows[i][1].ToString()), int.Parse(all.Rows[i][2].ToString())));
            }
            return toReturn;
        }

        public static List<XPLevel> GetLevels(ulong guild) =>
            new SqlInstance().Read("select * from @p0level_role where guild=@p1", guild).Bind<XPLevel>();

        public static bool AddLevel(XPLevel level)
        {
            var levels = GetLevels(level.Guild);
            if (levels.Any(l => l.Guild == level.Guild && l.Level == level.Level) || levels.Count > 9) { return false; }
            new SqlInstance().Execute("insert into @p0level_role values (@p1, @p2, @p3)", level.Guild, level.Level, level.Role);
            return true;
        }

        public static bool DeleteLevel(XPLevel level)
        {
            var levels = GetLevels(level.Guild);
            if (!levels.Any(l => l.Guild == level.Guild && l.Level == level.Level)) { return false; }
            new SqlInstance().Execute("delete from @p0level_role where guild=@p1 and level = @p2", level.Guild, level.Level);
            return true;
        }
    }
}
