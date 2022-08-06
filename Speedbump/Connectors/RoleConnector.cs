namespace Speedbump
{
    public static class RoleConnector
    {
        public static List<ulong> GetRoles(ulong guild) =>
            new SqlInstance().Read("select role from @p0role where guild=@p1", guild).Rows.Select(r => ulong.Parse(r[0].ToString())).ToList();

        public static bool Add(ulong guild, ulong role) =>
            (long) new SqlInstance().Read("insert ignore into @p0role values (@p1, @p2); select row_count();", guild, role).Rows[0][0] > 0;

        public static bool Remove(ulong guild, ulong role) =>
            (long)new SqlInstance().Read("delete from @p0role where guild=@p1 and role=@p2; select row_count();", guild, role).Rows[0][0] > 0;
    }
}
