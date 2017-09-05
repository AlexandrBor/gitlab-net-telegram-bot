using Microsoft.EntityFrameworkCore;

namespace GitlabTelegramBot.DB
{
    public class TelegramBotDBContext : DbContext
    {
        public DbSet<TelegramBotUser> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=GitlabBot.db");
        }
    }
}
