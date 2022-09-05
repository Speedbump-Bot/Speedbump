using Microsoft.EntityFrameworkCore;

namespace Speedbump
{
    public class DataContext : DbContext
    {
        public static IConfiguration Config;

        public DbSet<XPLevel> XPLevels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<XPLevel>()
                .HasKey(x => new { x.Guild, x.Level });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Config.Get<string>("mssql.connString"));
        }
    }
}
