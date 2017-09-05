using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class MergeParams
    {
        [JsonProperty("force_remove_source_branch")]
        public String ForceRemoveSourceBranch { get; set; }
    }
}
