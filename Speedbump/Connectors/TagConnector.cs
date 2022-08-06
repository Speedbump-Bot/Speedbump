namespace Speedbump
{
    public static class TagConnector
    {
        public static Tag GetByNameAndGuild(string name, ulong guild)
        {
            var i = new SqlInstance();
            var set = i.Read("select * from @p0tag where name=@p1 and guild=@p2", name, guild);
            if (set.RowCount == 0)
            {
                return null;
            }
            return set.Bind<Tag>()[0];
        }

        public static List<Tag> GetByGuild(ulong guild)
        {
            var i = new SqlInstance();
            var set = i.Read("select * from @p0tag where guild=@p1", guild);
            return set.Bind<Tag>();
        }

        public static void Create(Tag t)
        {
            var i = new SqlInstance();
            i.Execute("insert into @p0tag values (@p1, @p2, @p3, @p4, @p5)", t.TagID, t.Guild, t.Name, t.Template, t.Attachment);
        }

        public static bool Delete(ulong guild, string name)
        {
            var t = GetByNameAndGuild(name, guild);
            if (t is null) { return false; }
            var i = new SqlInstance();
            i.Execute("delete from @p0tag where guild=@p1 and name=@p2", guild, name);

            return true;
        }
    }
}
