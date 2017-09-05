using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class Note
    {
        [JsonProperty("object_kind")]
        public String ObjectKind { get; set; }
        [JsonProperty("user")]
        public User User { get; set; }
        [JsonProperty("project_id")]
        public Int32 ProjectId { get; set; }
        [JsonProperty("project")]
        public Source Project { get; set; }
        [JsonProperty("object_attributes")]
        public NoteBody Body { get; set; }
        [JsonProperty("repository")]
        public Repository Repository { get; set; }
        [JsonProperty("merge_request")]
        public MergeRequestBody MergeRequest { get; set; }
    }
}
