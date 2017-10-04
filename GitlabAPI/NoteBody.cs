using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class NoteBody
    {
        [JsonProperty("id")]
        public Int32 Id { get; set; }
        [JsonProperty("note")]
        public String Note { get; set; }
        [JsonProperty("noteable_type")]
        public String NoteableType { get; set; }
        [JsonProperty("author_id")]
        public Int32 AuthorId { get; set; }
        [JsonProperty("created_at")]
        public String CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public String UpdatedAt { get; set; }
        [JsonProperty("project_id")]
        public Int32 ProjectId { get; set; }
        [JsonProperty("attachment")]
        public String Attachment { get; set; }
        [JsonProperty("line_code")]
        public String LineCode { get; set; }
        [JsonProperty("commit_id")]
        public String CommitId { get; set; }
        [JsonProperty("noteable_id")]
        public Int32? NoteableId { get; set; }
        [JsonProperty("system")]
        public Boolean System { get; set; }
        [JsonProperty("st_diff")]
        public String StDiff { get; set; }
        [JsonProperty("updated_by_id")]
        public String UpdatedById { get; set; }
        [JsonProperty("type")]
        public String Type { get; set; }
        [JsonProperty("resolved_at")]
        public String ResolvedAt { get; set; }
        [JsonProperty("resolved_by_id")]
        public String ResolvedById { get; set; }
        [JsonProperty("discussion_id")]
        public String DiscussionId { get; set; }
        [JsonProperty("url")]
        public String Url { get; set; }
    }
}
