using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class Author
    {
        [JsonProperty("name")]
        public String Name { get; set; }
        [JsonProperty("email")]
        public String Email { get; set; }
    }
}
