using System.Collections.Generic;
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

        public void Migrate()
        {
            var users = new List<TelegramBotUser>()
            {
                new TelegramBotUser() { ChatId = 149901166, GitlabUserName = "MiryanovSN", TelegramName = "zzzzzzerg"},
                new TelegramBotUser() { ChatId = 103469385, GitlabUserName = "alyokhinaa", TelegramName = "zzanderss"},
                new TelegramBotUser() { ChatId = 293361925, GitlabUserName = "NugumanovDM", TelegramName = "hant111"},
                new TelegramBotUser() { ChatId = 93020512, GitlabUserName = "HolodnovJeJe", TelegramName = "HolodnovJeJe"},
                new TelegramBotUser() { ChatId = 400611109, GitlabUserName = "GazimullinDM", TelegramName = "GazimullinDM" },
                new TelegramBotUser() { ChatId = 122671309, GitlabUserName = "kovalevasr", TelegramName = "jam_of_sweet" },
                new TelegramBotUser() { ChatId = 202868276, GitlabUserName = "bashirovia", TelegramName = "Bashirov_Ildar" },
                new TelegramBotUser() { ChatId = 438941600, GitlabUserName = "FilkinDI", TelegramName = "DmitryFilkin" }
            };

            Users.AddRange(users);
            SaveChanges();
        }
    }
}
