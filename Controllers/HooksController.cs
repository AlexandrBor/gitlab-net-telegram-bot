using System;
using System.IO;
using System.Threading.Tasks;
using GitlabTelegramBot.MessageQueue;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitlabTelegramBot.Controllers
{
    [Route("hooks")]
    public class HooksController : Controller
    {
        public HooksController(ILogger<HooksController> logger, IMessagesQueue messageQueue)
        {
            _logger = logger;
            _messageQueue = messageQueue;
        }

        [HttpPost]
        public async Task CatchHook()
        {
            try
            {
                var body = HttpContext.Request.Body;
                using (var reader = new StreamReader(body))
                using (var jreader = new JsonTextReader(reader))
                {
                    var content = JToken.ReadFrom(jreader);
                    _logger.LogInformation($"Catched new hook: {content.ToString()}");
                    _messageQueue.Add(content);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(), e, "Unhandled error");
            }
        }

        private readonly ILogger<HooksController> _logger;
        private readonly IMessagesQueue _messageQueue;
    }
}
