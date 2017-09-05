using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class Source
    {
        [JsonProperty("name")]
        public String Name { get; set; }
        [JsonProperty("description")]
        public String Description { get; set; }
        [JsonProperty("web_url")]
        public String WebUrl { get; set; }
        [JsonProperty("avatar_url")]
        public String AvatarUrl { get; set; }
        [JsonProperty("git_ssh_url")]
        public String GitSshUrl { get; set; }
        [JsonProperty("git_http_url")]
        public String GitHttpUrl { get; set; }
        [JsonProperty("namespace")]
        public string Namespace { get; set; }
        [JsonProperty("visibility_level")]
        public Int32 VisibilityLevel { get; set; }
        [JsonProperty("path_with_namespace")]
        public String PathWithNamespace { get; set; }
        [JsonProperty("default_branch")]
        public String DefaultBranch { get; set; }
        [JsonProperty("homepage")]
        public String Homepage { get; set; }
        [JsonProperty("ci_config_path")]
        public String CiConfigPath { get; set; }
        [JsonProperty("url")]
        public String Url { get; set; }
        [JsonProperty("ssh_url")]
        public String SshUrl { get; set; }
        [JsonProperty("http_url")]
        public String HttpUrl { get; set; }
    }
}
