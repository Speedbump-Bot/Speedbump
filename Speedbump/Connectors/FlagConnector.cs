namespace Speedbump
{
    public static class FlagConnector
    {
        public static int GetPointsByUserInGuild(ulong guild, ulong user, DateTime start, DateTime end)
        {
            var i = new OldSqlInstance();
            return Decimal.ToInt32((decimal)i.Read(
                @"select
	                coalesce(sum(resolution_points), 0)
                from @p0flag
                where guild = @p1
                and source_user = @p2
                and time between @p3 and @p4",
                guild, user, start, end).Rows[0][0]);
        }

        public static int GetCountByUserInGuild(ulong guild, ulong user, DateTime start, DateTime end)
        {
            var i = new OldSqlInstance();
            return Decimal.ToInt32((long)i.Read(
                @"select
	                count(*)
                from @p0flag
                where guild = @p1
                and source_user = @p2
                and time between @p3 and @p4
                and resolution_points > 0",
                guild, user, start, end).Rows[0][0]);
        }

        public static Flag Create(Flag f, ulong user)
        {
            f.ID = Snowflake.Generate();
            var i = new OldSqlInstance();
            i.Execute("insert into @p0flag values (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, " +
                "@p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17)",
                f.ID, f.Guild, f.Message, f.Time, f.Type, f.FlaggedBy, f.SourceMessage, f.SourceUser, f.SourceContent,
                f.SourceMatches, f.SourceChannel, f.SourceGuild, f.ResolutionUser, f.ResolutionTime, f.ResolutionType, 
                f.ResolutionPoints, f.SystemMessage);
            i.Execute("insert into @p0flag_history values (@p1, @p2, @p3, @p4)", f.ID, f.ResolutionType, DateTime.Now, user);
            return f;
        }

        public static Flag GetByMessage(ulong message)
        {
            var i = new OldSqlInstance();
            return i.Read("select * from @p0flag where message=@p1", message).Bind<Flag>()[0];
        }

        public static Flag UpdateFlag(Flag f, ulong user)
        {
            var i = new OldSqlInstance();
            i.Execute("delete from @p0flag where id=@p1", f.ID);
            i.Execute("insert into @p0flag values (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, " +
                "@p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17)",
                f.ID, f.Guild, f.Message, f.Time, f.Type, f.FlaggedBy, f.SourceMessage, f.SourceUser, f.SourceContent,
                f.SourceMatches, f.SourceChannel, f.SourceGuild, f.ResolutionUser, f.ResolutionTime, f.ResolutionType,
                f.ResolutionPoints, f.SystemMessage);
            i.Execute("insert into @p0flag_history values (@p1, @p2, @p3, @p4)", f.ID, f.ResolutionType, DateTime.Now, user);
            return f;
        }

        public static List<FlagHistory> GetHistory(Flag f) => new OldSqlInstance().Read("select * from @p0flag_history where flag=@p1", f.ID).Bind<FlagHistory>();
    }
}
