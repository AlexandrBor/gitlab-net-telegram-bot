using System;
using Newtonsoft.Json;

namespace GitlabTelegramBot.GitlabAPI
{
    public class MergeRequestBody
    {
        [JsonProperty("id")]
        public Int32 Id { get; set; }
        [JsonProperty("target_branch")]
        public String TargetBranch { get; set; }
        [JsonProperty("source_branch")]
        public String SourceBranch { get; set; }
        [JsonProperty("source_project_id")]
        public Int32? SourceProjectId { get; set; }
        [JsonProperty("Author_id")]
        public Int32? AuthorId { get; set; }
        [JsonProperty("assignee_id")]
        public Int32? AssigneeId { get; set; }
        [JsonProperty("title")]
        public String Title { get; set; }
        [JsonProperty("created_at")]
        public String CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public String UpdatedAt { get; set; }
        [JsonProperty("st_commits")]
        public Object StCommits { get; set; }
        [JsonProperty("st_diffs")]
        public Object StDiffs { get; set; }
        [JsonProperty("milestone_id")]
        public Object MilestoneId { get; set; }
        [JsonProperty("state")]
        public String State { get; set; }
        [JsonProperty("merge_status")]
        public String MergeStatus { get; set; }
        [JsonProperty("target_project_id")]
        public Int32? TargetProjectId { get; set; }
        [JsonProperty("iid")]
        public Int32 Iid { get; set; }
        [JsonProperty("description")]
        public String Description { get; set; }
        [JsonProperty("source")]
        public Source Source { get; set; }
        [JsonProperty("target")]
        public Source Target { get; set; }
        [JsonProperty("last_commit")]
        public Commit LastCommit { get; set; }
        [JsonProperty("work_in_progress")]
        public Boolean WorkInProgress { get; set; }
        [JsonProperty("url")]
        public String Url { get; set; }
        [JsonProperty("action")]
        public String Action { get; set; }
        [JsonProperty("locked_at")]
        public String LockedAt { get; set; }
        [JsonProperty("updated_by_id")]
        public String UpdatedById { get; set; }
        [JsonProperty("merge_error")]
        public String MergeError { get; set; }
        [JsonProperty("merge_params")]
        public MergeParams MergeParams { get; set; }
        [JsonProperty("merge_when_pipeline_succeeds")]
        public Boolean MergeWhenPipelineSucceeds { get; set; }
        [JsonProperty("merge_user_id")]
        public String MergeUserId { get; set; }
        [JsonProperty("merge_commit_sha")]
        public String MergeCommitSha { get; set; }
        [JsonProperty("deleted_at")]
        public String DeletedAt { get; set; }
        [JsonProperty("in_progress_merge_commit_sha")]
        public String InProgressMergeCommitSha { get; set; }
        [JsonProperty("lock_version")]
        public String LockVersion { get; set; }
        [JsonProperty("time_estimate")]
        public Int32? TimeEstimate { get; set; }
        [JsonProperty("last_edited_at")]
        public String LastEditedAt { get; set; }
        [JsonProperty("last_edited_by_id")]
        public String LastEditedById { get; set; }
        [JsonProperty("head_pipeline_id")]
        public Int32? HeadPipelineId { get; set; }
        [JsonProperty("ref_fetched")]
        public Boolean RefFetched { get; set; }
        [JsonProperty("total_time_spent")]
        public Int32? TotalTimeSpent { get; set; }
        [JsonProperty("human_total_time_spent")]
        public Int32? HumanTotalTimeSpent { get; set; }
        [JsonProperty("human_time_estimate")]
        public Int32? HumanTimeEstimate { get; set; }
    }
}
