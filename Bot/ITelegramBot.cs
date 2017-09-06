using System.Collections.Generic;
using System.Threading.Tasks;
using GitlabTelegramBot.DB;

namespace GitlabTelegramBot
{
    public interface ITelegramBot
    {
        void Connect(string accessToken, string name);
        void Start();
        void Stop();

        Task SendMessage(IEnumerable<TelegramBotUser> user, string message);
    }
}
