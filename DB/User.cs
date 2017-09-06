using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GitlabTelegramBot.DB
{
    public class TelegramBotUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ID { get; set; }
        public Int64 ChatId { get; set; }
        public String TelegramName { get; set; }
        public String GitlabUserName { get; set; }

        public Boolean IsRegistered()
        {
            if (string.IsNullOrEmpty(TelegramName) || string.IsNullOrEmpty(GitlabUserName))
            {
                return false;
            }
            return true;
        }
    }
}
