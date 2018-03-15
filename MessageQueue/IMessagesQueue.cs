using Newtonsoft.Json.Linq;

namespace GitlabTelegramBot.MessageQueue
{
    public interface IMessagesQueue
    {
        /// <summary>
        /// Запускает обработку очереди сообщений
        /// </summary>
        void Start();

        /// <summary>
        /// Останавливает обработку очереди сообщений
        /// </summary>
        void Stop();

        /// <summary>
        /// Добавляет сообщение в очередь
        /// </summary>
        /// <param name="request">Сообщение</param>
        void Add(JToken request);
    }
}
