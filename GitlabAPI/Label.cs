using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class Label
    {
        [JsonProperty("id")]
        public Int32 Id { get; set; }

        [JsonProperty("title")]
        public String Title { get; set; }

        [JsonProperty("color")]
        public String Color { get; set; }

        [JsonProperty("project_id")]
        public Int32 ProjectId { get; set; }

        [JsonProperty("type")]
        public String Type { get; set; }
    }
}
