using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class Commit
    {
        [JsonProperty("id")]
        public String Id { get; set; }
        [JsonProperty("message")]
        public String Message { get; set; }
        [JsonProperty("timestamp")]
        public String Timestamp { get; set; }
        [JsonProperty("url")]
        public String Url { get; set; }
        [JsonProperty("author")]
        public Author Author { get; set; }
        [JsonProperty("added")]
        public List<String> Added { get; set; }
        [JsonProperty("modified")]
        public List<String> Modified { get; set; }
        [JsonProperty("removed")]
        public List<String> Removed { get; set; }
    }
}
