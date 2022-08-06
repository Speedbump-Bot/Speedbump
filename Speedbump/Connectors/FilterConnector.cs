namespace Speedbump
{
    public static class FilterConnector
    {
        static DataCache<List<FilterMatch>> Matches;
        static FilterConnector()
        {
            Matches = new (() =>
            {
                return GetFilterMatches();
            }, TimeSpan.FromSeconds(60));
        }

        private static List<FilterMatch> GetFilterMatches() =>
            new SqlInstance().Read(@"select * from @p0filter").Bind<FilterMatch>();

        public static List<FilterMatch> GetMatches(ulong guild) => Matches.Data.Where(m => m.Guild == guild).ToList();

        public static bool AddMatch(FilterMatch f)
        {
            var i = new SqlInstance();
            var m = i.Read("select * from @p0filter where guild=@p1 and `match`=@p2", f.Guild, f.Match);
            if (m.RowCount > 0)
            {
                return false;
            }

            i.Execute("insert into @p0filter values (@p1, @p2, @p3)", f.Guild, f.Match, f.Type);
            Matches.Invalidate();

            return true;
        }

        public static bool RemoveMatch(ulong guild, string match)
        {
            var res = (long)new SqlInstance().Read("delete from @p0filter where guild=@p1 and `match`=@p2; select row_count();", guild, match).Rows[0][0] > 0;
            Matches.Invalidate();
            return res;
        }
    }
}
