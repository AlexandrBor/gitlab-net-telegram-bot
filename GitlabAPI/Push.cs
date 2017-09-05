using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class Push
    {
        [JsonProperty("object_kind")]
        public String ObjectKind { get; set; }
        [JsonProperty("before")]
        public String Before { get; set; }
        [JsonProperty("after")]
        public String After { get; set; }
        [JsonProperty("ref")]
        public String Ref { get; set; }
        [JsonProperty("checkout_sha")]
        public String CheckoutSha { get; set; }
        [JsonProperty("user_id")]
        public Int32 UserId { get; set; }
        [JsonProperty("user_name")]
        public String UserName { get; set; }
        [JsonProperty("user_username")]
        public String UserUsername { get; set; }
        [JsonProperty("user_email")]
        public String UserEmail { get; set; }
        [JsonProperty("user_avatar")]
        public String UserAvatar { get; set; }
        [JsonProperty("project_id")]
        public Int32 ProjectId { get; set; }
        [JsonProperty("project")]
        public Project Project { get; set; }
        [JsonProperty("repository")]
        public Repository Repository { get; set; }
        [JsonProperty("commits")]
        public List<Commit> Commits { get; set; }
        [JsonProperty("total_commits_count")]
        public Int32 TotalCommitsCount { get; set; }
    }

}
