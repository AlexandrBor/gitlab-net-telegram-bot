using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class Repository
    {
        [JsonProperty("name")]
        public String Name { get; set; }
        [JsonProperty("url")]
        public String Url { get; set; }
        [JsonProperty("description")]
        public String Description { get; set; }
        [JsonProperty("homepage")]
        public String Homepage { get; set; }
        [JsonProperty("git_http_url")]
        public String GitHttpUrl { get; set; }
        [JsonProperty("git_ssh_url")]
        public String GitSshUrl { get; set; }
        [JsonProperty("visibility_level")]
        public Int32 VisibilityLevel { get; set; }
    }
}
