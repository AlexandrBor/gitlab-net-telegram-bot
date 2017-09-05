using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class User
    {
        [JsonProperty("name")]
        public String Name { get; set; }
        [JsonProperty("username")]
        public String Username { get; set; }
        [JsonProperty("avatar_url")]
        public String AvatarUrl { get; set; }
    }
}
