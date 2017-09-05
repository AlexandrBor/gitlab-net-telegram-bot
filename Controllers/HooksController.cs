using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitlabTelegramBot.DB;
using GitlabTelegramBot.GitlabAPI;
using GitlabTelegramBot.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NGitLab;

namespace GitlabTelegramBot.Controllers
{
    [Route("hooks")]
    public class HooksController : Controller
    {
        public HooksController(ILogger<HooksController> logger, ITelegramBot bot, IOptions<GitlabConfig> gitlabConfig, TelegramBotDBContext db)
        {
            _logger = logger;
            _bot = bot;
            _db = db;
            var host = gitlabConfig.Value.Host;
            var privateToken = gitlabConfig.Value.Token;
            _gitlab = new GitLabClient(host, privateToken);
            _admin = gitlabConfig.Value.Admin;
        }

        [HttpPost]
        public async Task CatchHook()
        {
            var body = HttpContext.Request.Body;
            using (var reader = new StreamReader(body))
            using (var jreader = new JsonTextReader(reader))
            {
                var content = JToken.ReadFrom(jreader);
                var kind = content.Value<String>("object_kind");
                switch (kind)
                {
                    case "note":
                        {

                            var note = content.ToObject<Note>();
                            await NewNote(note);
                            return;
                        }
                    case "push":
                        {
                            var push = content.ToObject<Push>();
                            await NewPush(push);
                            return;
                        }
                    case "merge_request":
                        {
                            var mergeRequest = content.ToObject<MergeRequest>();
                            await NewMergeRequest(mergeRequest);
                            return;
                        }
                };
            }
        }

        public async Task NewPush(Push push)
        {
            var branch = GetBranch(push);
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
                    message = $"@{user.TelegramName} push new branch: {branch}";
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

        public async Task NewMergeRequest(MergeRequest mergeRequest)
        {
            var gitlabUsers = new List<String>();
            gitlabUsers.Add(mergeRequest.User.Username);
            if (mergeRequest.Body.Assignee != null && !gitlabUsers.Contains(mergeRequest.Body.Assignee.Username))
            {
                gitlabUsers.Add(mergeRequest.Body.Assignee.Username);
            }
            var users = _db.Users.Where(_ => gitlabUsers.Contains(_.GitlabUserName)).ToArray();
            var user = users.FirstOrDefault(_ => _.GitlabUserName == mergeRequest.User.Username);

            var issueUrl = GetRedmineURL(mergeRequest.Body.Title);
            var message = $"@{user.TelegramName} {mergeRequest.Body.Action} merge request\n{mergeRequest.Body.Title}\n{issueUrl}\n{mergeRequest.Body.Url}";

            await _bot.SendMessage(users, message);
        }

        public async Task NewNote(Note note)
        {
            var gitlabUsers = new List<String>();
            gitlabUsers.AddRange(GetAllUsers(note.Body.Note));
            if (note.User != null)
            {
                gitlabUsers.Add(note.User.Username);
            }

            if (note.MergeRequest.AuthorId.HasValue)
            {
                var author = (await _gitlab.Users.All()).FirstOrDefault(_ => _.Id == note.MergeRequest.AuthorId.Value);
                if (author != null && !gitlabUsers.Contains(author.Username))
                {
                    gitlabUsers.Add(author.Username);
                }
            }

            var redmineURL = GetRedmineURL(note.MergeRequest.Title);
            var users = _db.Users.Where(_ => gitlabUsers.Contains(_.GitlabUserName)).ToArray();
            var user = users.FirstOrDefault(_ => _.GitlabUserName == note.User.Username);
            var message = $"@{user?.TelegramName}: {note.Body.Note}\n{redmineURL}\n{note.Body.Url}";
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
                users.Add(match.Value);
            }
            return users;
        }

        private readonly ILogger<HooksController> _logger;
        private readonly ITelegramBot _bot;
        private readonly GitLabClient _gitlab;
        private readonly TelegramBotDBContext _db;
        private readonly String _admin;
    }
}
