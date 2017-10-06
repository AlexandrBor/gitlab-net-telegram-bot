using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitlabTelegramBot.DB;
using GitlabTelegramBot.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using NGitLab;
using NGitLab.Models;

namespace GitlabTelegramBot
{
    public class Bot : ITelegramBot
    {
        public Bot(ILogger<Bot> logger, TelegramBotDBContext context, IOptions<GitlabConfig> gitlabConfig)
        {
            _logger = logger;
            _context = context;
            _newUsers = new List<TelegramBotUser>();
            _gitlab = new GitLabClient(gitlabConfig.Value.Host, gitlabConfig.Value.Token);
        }

        public void Connect(string accessToken, string name)
        {
            if (accessToken != null)
            {
                _botName = name;
                _logger.LogInformation($"Connected TelegramBot {name} with token: {accessToken}");
                _bot = new TelegramBot(accessToken);
                _cts = new CancellationTokenSource();
                if (!CheckConnect().Result)
                {
                    _cts.Cancel();
                }
            }
            else
            {
                _logger.LogError("Connect to telegramBot failed because accessToken not found");
            }
        }

        private async Task<Boolean> CheckConnect()
        {
            var me = await _bot.MakeRequestAsync(new GetMe());
            if (me != null)
            {
                _logger.LogInformation("{0} (@{1}) connected!", me.FirstName, me.Username);
                return true;
            }

            _logger.LogInformation("Bot connected failed!");
            return false;
        }

        public void Start()
        {
            _logger.LogInformation("TelegramBot start listening");
            Task.Factory.StartNew(() => Listening(), TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }
        }

        private async Task Listening()
        {
            long offset = 0;
            while (!_cts.IsCancellationRequested)
            {
                var updates = await _bot.MakeRequestAsync(new GetUpdates() { Offset = offset });
                if (updates != null)
                {
                    foreach (var update in updates)
                    {
                        var text = update.Message.Text;
                        var chatId = update.Message.Chat.Id;
                        var newUser = _newUsers.FirstOrDefault(_ => _.TelegramName == update.Message.Chat.Username);
                        _logger.LogInformation($"New message from chat: {update.Message.Chat.Id} with user: {update.Message.Chat.Username} text: {update.Message.Text}");
                        if (text == "/start")
                        {
                            await HelpCommand(chatId);
                        }
                        else if (text == "/register" || (newUser != null && !newUser.IsRegistered()))
                        {
                            await RegisterCommand(update.Message);
                        }
                        else if (text == "/help")
                        {
                            await HelpCommand(chatId);
                        }
                        else if (text == "/stop")
                        {
                            await UnregisterCommand(update.Message);
                        }
                        else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.From.Username == _botName)
                        {
                            await ReplyToMessage(update.Message);
                        }
                        offset = update.UpdateId + 1;
                    }
                }
            }
        }

        private async Task RegisterCommand(Message message)
        {
            var chat = message.Chat;
            _logger.LogInformation($"New request for register from chat: {message.Chat.Id} with user: {message.Chat.Username}");
            if (_context.Users.Any(_ => _.TelegramName == chat.Username))
            {
                var req = new SendMessage(chat.Id, $"Hello {chat.FirstName} {chat.LastName}. You already registered!");
                _logger.LogInformation($"Register request is rejected. User with {chat.FirstName} {chat.LastName} already registered");
                await _bot.MakeRequestAsync(req);
                return;
            }
            var newUser = _newUsers.FirstOrDefault(_ => _.TelegramName == chat.Username);
            if (newUser == null)
            {
                var req = new SendMessage(chat.Id, $"Hello {chat.FirstName} {chat.LastName}. Whats you name in gitlab?");
                await _bot.MakeRequestAsync(req);
                _newUsers.Add(new TelegramBotUser() { TelegramName = chat.Username, ChatId = chat.Id });
            }
            else if (string.IsNullOrEmpty(newUser.GitlabUserName))
            {
                newUser.GitlabUserName = message.Text;
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                _newUsers.Remove(newUser);
                var req = new SendMessage(chat.Id, $"Very well! Now you registered");
                await _bot.MakeRequestAsync(req);
                _logger.LogInformation($"Registered new user from chat: {newUser.ChatId} TelegramUserName: {newUser.TelegramName}  GitlabUserName:{newUser.GitlabUserName}");
            }
        }

        private async Task UnregisterCommand(Message message)
        {
            _logger.LogInformation($"New request for unregister from chat: {message.Chat.Id} with user: {message.Chat.Username}");
            var chat = message.Chat;
            var user = _context.Users.FirstOrDefault(_ => _.TelegramName == chat.Username);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                var req = new SendMessage(chat.Id, $"{chat.FirstName} {chat.LastName} deleted from bot");
                await _bot.MakeRequestAsync(req);
                _logger.LogInformation($"User deleted: Chat: {user.ChatId} TelegramName: {user.TelegramName} GitlabName: {user.GitlabUserName}");
            }
            else
            {
                var req = new SendMessage(chat.Id, $"You are not registered yet");
                await _bot.MakeRequestAsync(req);
                _logger.LogInformation($"Unregister rejected, user not found. Chat: {message.Chat.Id} TelegramUser: {message.Chat.Username}");
            }
        }

        private async Task HelpCommand(long chatId)
        {
            var keyb = new ReplyKeyboardMarkup()
            {
                Keyboard = new[] { new[] { new KeyboardButton("/register"), new KeyboardButton("/stop") }, new[] { new KeyboardButton("/help") } },
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
            var reqAction = new SendMessage(chatId, "Here is all my commands") { ReplyMarkup = keyb };
            await _bot.MakeRequestAsync(reqAction);
        }

        public async Task SendMessage(IEnumerable<TelegramBotUser> users, string message)
        {
            try
            {
                foreach (var user in users)
                {
                    _logger.LogInformation($"Send message: '{message}' chat: {user.ChatId} TelegramUser: {user.TelegramName}");
                    await _bot.MakeRequestAsync(new SendMessage(user.ChatId, message));
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error in sending message from bot", e);
            }
        }

        private async Task ReplyToMessage(Message message)
        {
            var noteId = GetNoteIdFromMessage(message.ReplyToMessage.Text);
            var user = _context.Users.FirstOrDefault(_ => _.ChatId == message.Chat.Id);
            if (user != null)
            {
                var gitlabUser = (await _gitlab.Users.All()).FirstOrDefault(_ => _.Username == user.GitlabUserName);
                if (gitlabUser != null)
                {
                    var comment = new MergeRequestComment() { Author = gitlabUser, Body = message.Text };
                    var mergeRequestId = GetMergeRequestIdFromMessage(message.ReplyToMessage.Text);
                    if (!string.IsNullOrEmpty(mergeRequestId))
                    {
                        var id = Int32.Parse(mergeRequestId);
                        var mergeRequest = _gitlab.GetMergeRequest(id);
                        await mergeRequest.Comments(id).AddAsync(comment).ConfigureAwait(false);
                    }
                }
            }
        }

        private String GetMergeRequestIdFromMessage(string text)
        {
            var rgx = new Regex("/merge_requests/[0-9]+");
            var result = rgx.Match(text).Value;
            var tokens = result.Split('/');
            if (tokens.Length == 3)
            {
                return tokens.LastOrDefault();
            }
            return string.Empty;
        }

        private String GetNoteIdFromMessage(string text)
        {
            var rgx = new Regex("#note_[0-9]+");
            return rgx.Match(text).Value; ;
        }

        private List<TelegramBotUser> _newUsers;

        private readonly ILogger<Bot> _logger;
        private readonly TelegramBotDBContext _context;
        private readonly GitLabClient _gitlab;

        private TelegramBot _bot;
        private CancellationTokenSource _cts;
        private string _botName;
    }
}
