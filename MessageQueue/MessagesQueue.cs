﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitlabTelegramBot.DB;
using GitlabTelegramBot.GitlabAPI;
using GitlabTelegramBot.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NGitLab;

namespace GitlabTelegramBot.MessageQueue
{
    public class MessagesQueue : IMessagesQueue
    {
        public MessagesQueue(ILogger<MessagesQueue> logger, ITelegramBot bot, IOptions<GitlabConfig> gitlabConfig, TelegramBotDBContext db)
        {
            _logger = logger;
            _bot = bot;
            _db = db;
            _admin = gitlabConfig.Value.Admin;
            _queue = new Queue<JToken>();
            _gitlab = new GitLabClient(gitlabConfig.Value.Host, gitlabConfig.Value.Token);
        }

        public void Add(JToken request)
        {
            _queue.Enqueue(request);
        }

        public void Start()
        {
            _ct = new CancellationTokenSource();
            Task.Factory.StartNew(() => DoWork(), _ct.Token);
        }

        public void Stop()
        {
            _ct.Cancel();
        }

        private async Task DoWork()
        {
            while (!_ct.IsCancellationRequested)
            {
                while (_queue.Count > 0)
                {
                    await Task.Factory.StartNew(() => PrepareRequest(_queue.Dequeue()));
                }

                await Task.Delay(_sleepTime);
            }
        }

        private async Task PrepareRequest(JToken content)
        {
            var kind = content.Value<String>("object_kind");
            var serializer = new JsonSerializer();
            switch (kind)
            {
                case "note":
                    {
                        var note = content.ToObject<Note>(serializer);
                        await NewNote(note);
                        return;
                    }
                case "push":
                    {
                        var push = content.ToObject<Push>(serializer);
                        await NewPush(push);
                        return;
                    }
                case "merge_request":
                    {
                        var mergeRequest = content.ToObject<MergeRequest>(serializer);
                        await NewMergeRequest(mergeRequest);
                        return;
                    }
            };
        }

        private async Task NewPush(Push push)
        {
            var branch = GetBranch(push);
            _logger.LogInformation($"Prepare new push: {branch} from {push.UserUsername}");
            var user = _db.Users.FirstOrDefault(_ => _.GitlabUserName == push.UserUsername);
            var message = string.Empty;
            if (push.After == "0000000000000000000000000000000000000000")
            {
                message = $"{user.TelegramName} delete branch {branch}";
            }
            else
            {
                if (branch == "master")
                {
                    message = $"Project: {push.Project.Name}\r\n@{user.TelegramName} push new branch: {branch}";
                }
                else
                {
                    var mergeRequests = await _gitlab.GetMergeRequest(push.ProjectId).AllInState(NGitLab.Models.MergeRequestState.opened);
                    var mergeRequest = mergeRequests.FirstOrDefault(_ => _.SourceBranch == branch);
                    if (mergeRequest != null)
                    {
                        message = $"@{user.TelegramName} update branch: {branch}\n{push.Project.WebUrl}";
                    }
                    else
                    {
                        var requestUrl = $"{push.Project.WebUrl}/merge_requests/new?merge_request[source_branch]={branch}&merge_request[source_project_id]={push.ProjectId}&merge_request[target_branch]=master&merge_request[target_project_id]={push.ProjectId}";
                        message = $"@{user?.TelegramName} create new branch: {branch}\n{requestUrl}";
                    }
                }
            }

            if (!string.IsNullOrEmpty(message))
            {
                var users = new List<TelegramBotUser>() { user };
                var admin = _db.Users.FirstOrDefault(_ => _.TelegramName == _admin);
                if (admin != null)
                {
                    users.Add(admin);
                }
                await _bot.SendMessage(users, message);
            }
        }

        private async Task NewMergeRequest(MergeRequest mergeRequest)
        {
            _logger.LogInformation($"Prepare new merge request: {mergeRequest.Body.Id} action: {mergeRequest.Body.Action} from {mergeRequest.User.Username}");
            var gitlabUsers = new List<String>();
            gitlabUsers.Add(mergeRequest.User.Username);
            if (mergeRequest.Assignee != null && !gitlabUsers.Contains(mergeRequest.Assignee.Username))
            {
                gitlabUsers.Add(mergeRequest.Assignee.Username);
                _logger.LogInformation($"Add assignee from merge request: {mergeRequest.Assignee.Username}");
            }

            if (mergeRequest.Body.Action.ToUpper() == "MERGE" && mergeRequest.User != null)
            {
                gitlabUsers.Add(mergeRequest.User.Username);
                _logger.LogInformation($"Add author of merge request: {mergeRequest.User.Username}");
            }

            var users = _db.Users.Where(_ => gitlabUsers.Contains(_.GitlabUserName)).ToList();
            var user = users.FirstOrDefault(_ => _.GitlabUserName == mergeRequest.User.Username);
            var assignee = users.FirstOrDefault(_ => _.GitlabUserName == mergeRequest.Assignee.Username);

            var author = (await  _gitlab.Users.All()).FirstOrDefault(_ => _.Id == mergeRequest.Body.AuthorId);
            var authorDB = author != null ? _db.Users.FirstOrDefault(_ => _.GitlabUserName == author.Username) : null;
            if (authorDB != null && !users.Contains(authorDB))
            {
                users.Add(authorDB);
            }

            var userName = user != null ? user.TelegramName : mergeRequest.User.Username;
            var assigneeName = assignee != null ? assignee.TelegramName : mergeRequest.Assignee.Name;
            var authorName = authorDB != null ? authorDB.TelegramName : userName;
            var redmineURL = GetRedmineURL(mergeRequest.Body.Title);
            var message = string.Empty;
            if (string.IsNullOrEmpty(redmineURL))
            {
                if (mergeRequest.Body.Action == "update" && mergeRequest.Labels.Length > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var label in mergeRequest.Labels)
                    {
                        sb.AppendLine($"Label: {label.Title}");
                    }
                    message = $"@{authorName} @{assigneeName}\r\nProject: {mergeRequest.Body.Source.Name}\r\n{sb.ToString()}{mergeRequest.Body.Url}";
                }
                else
                {
                    message = $"@{authorName} {mergeRequest.Body.Action} merge request\n{mergeRequest.Body.Title}\n{mergeRequest.Body.Url}";
                }
            }
            else
            {
                _logger.LogInformation($"Found redmine URL: {redmineURL}");

                if (mergeRequest.Body.Action == "update" && mergeRequest.Labels.Length > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var label in mergeRequest.Labels)
                    {
                        sb.AppendLine($"Label: {label.Title}");
                    }
                    message = $"@{authorName} @{assigneeName}\r\nProject: {mergeRequest.Body.Source.Name}\r\n{sb.ToString()}{redmineURL}\r\n{mergeRequest.Body.Url}";
                }
                else
                {
                    message = $"@{userName}\r\nProject: {mergeRequest.Body.Source.Name} {mergeRequest.Body.Action} merge request\n{mergeRequest.Body.Title}\n{redmineURL}\n{mergeRequest.Body.Url}";
                }
            }

            await _bot.SendMessage(users, message);
        }

        private async Task NewNote(Note note)
        {
            _logger.LogInformation($"Prepare new note: {note.Body.NoteableId} from {note.User.Username}");
            var gitlabUsers = new List<String>();
            string redmineURL = string.Empty;
            var usersInNote = GetAllUsers(note.Body.Note);
            if (usersInNote.Count > 0)
            {
                _logger.LogInformation($"Found direct call note user: {string.Join(",", usersInNote)}");
            }
            gitlabUsers.AddRange(usersInNote);

            var allGitlabUsers = await _gitlab.Users.All();

            if (note.MergeRequest != null)
            {
                if (note.MergeRequest.AuthorId.HasValue)
                {
                    var author = allGitlabUsers.FirstOrDefault(_ => _.Id == note.MergeRequest.AuthorId.Value);
                    if (author != null && note.User.Username != author.Username && !gitlabUsers.Contains(author.Username))
                    {
                        _logger.LogInformation($"Add author to note users: {author.Username}");
                        gitlabUsers.Add(author.Username);
                    }
                }
                if (note.MergeRequest.AssigneeId.HasValue)
                {
                    var assignee = allGitlabUsers.FirstOrDefault(_ => _.Id == note.MergeRequest.AssigneeId.Value);
                    if (assignee != null && note.User.Username != assignee.Username && !gitlabUsers.Contains(assignee.Username))
                    {
                        _logger.LogInformation($"Add assignee to note users: {assignee.Username}");
                        gitlabUsers.Add(assignee.Username);
                    }
                }
                redmineURL = GetRedmineURL(note.MergeRequest.Title);
            }
            else if (note.Commit != null && note.Commit.Author != null)
            {
                var author = allGitlabUsers.FirstOrDefault(_ => _.Email.ToUpper() == note.Commit.Author.Email.ToUpper());
                if (author != null && author.Username != note.User.Username)
                {
                    _logger.LogInformation($"Add commit author to note users: {author.Username}");
                    gitlabUsers.Add(author.Username);
                }
            }

            var users = _db.Users.Where(_ => gitlabUsers.Contains(_.GitlabUserName)).ToArray();
            var user = users.FirstOrDefault(_ => _.GitlabUserName == note.User.Username);
            var username = user == null ? note.User.Username : user.TelegramName;
            var message = string.Empty;

            if (string.IsNullOrEmpty(redmineURL))
            {
                message = $"@{username}: {note.Body.Note}\n{note.Body.Url}";
            }
            else
            {
                _logger.LogInformation($"Found redmine URL: {redmineURL}");
                message = $"@{username}: {note.Body.Note}\n{redmineURL}\n{note.Body.Url}";
            }
            await _bot.SendMessage(users, message);
        }

        private String GetBranch(Push push)
        {
            var tokens = push.Ref.Split('/');
            if (tokens.Length == 3)
            {
                return push.Ref.Split('/')[2];
            }
            return string.Empty;
        }

        private String GetRedmineURL(string content)
        {
            var rgx = new Regex("#[0-9]+", RegexOptions.IgnoreCase);
            var redmineTaskId = rgx.Match(content).Value.Replace("#", string.Empty);
            if (!string.IsNullOrEmpty(redmineTaskId))
            {
                return $"http://redmine.ufntc.ru/issues/{redmineTaskId}";
            }
            return string.Empty;
        }

        private List<String> GetAllUsers(string content)
        {
            var users = new List<String>();
            var rgx = new Regex("@\\S+");
            foreach (Match match in rgx.Matches(content))
            {
                users.Add(match.Value.Replace("@", string.Empty));
            }
            return users;
        }

        private static int _sleepTime = 5000;

        private CancellationTokenSource _ct;

        private readonly Queue<JToken> _queue;
        private readonly ILogger<MessagesQueue> _logger;
        private readonly ITelegramBot _bot;
        private readonly GitLabClient _gitlab;
        private readonly TelegramBotDBContext _db;
        private readonly string _admin;
    }
}
