using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class MergeRequest
    {
        [JsonProperty("object_kind")]
        public String ObjectKind { get; set; }
        [JsonProperty("user")]
        public User User { get; set; }
        [JsonProperty("object_attributes")]
        public MergeRequestBody Body { get; set; }
        [JsonProperty("assignee")]
        public User Assignee { get; set; }
        [JsonProperty("labels")]
        public Label[] Labels { get; set; }
    }
}
