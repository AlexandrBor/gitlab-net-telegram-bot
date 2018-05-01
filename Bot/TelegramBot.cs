using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using GitlabTelegramBot.DB;
using GitlabTelegramBot.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using NGitLab;
using NGitLab.Models;
using Extreme.Net;

namespace GitlabTelegramBot
{
    public class CustomTelegramBot : TelegramBot
    {
        public Int32 ProxyIndex
        {
            get => _proxyIndex;
            set
            {
                _proxyIndex = value;
                if (_proxyIndex >= _config.Proxies.Length)
                    _proxyIndex = 0;
            }
        }

        public ProxyConfig Proxy
        {
            get { return _config.Proxies[ProxyIndex]; }
        }

        public CustomTelegramBot(String accessToken, ProxiesConfig config)
        : base(accessToken)
        {
            _config = config;
            ProxyIndex = 0;
        }

        protected override HttpClientHandler MakeHttpMessageHandler()
        {
            var config = _config.Proxies[ProxyIndex];

            if (config.Enabled)
            {
                var sp = new Socks5ProxyClient(config.Host,
                    config.Port,
                    config.UserName,
                    config.Password)
                {
                    ConnectTimeout = 5000,
                    ReadWriteTimeout = 1000
                };

                return new ProxyHandler(sp);
            }
            else
            {
                return base.MakeHttpMessageHandler();
            }
        }

        private readonly ProxiesConfig _config;
        private int _proxyIndex;
    }

    public class Bot : ITelegramBot
    {
        public Bot(ILogger<Bot> logger,
            TelegramBotDBContext context,
            IOptions<ProxiesConfig> proxyConfig)
        {
            _logger = logger;
            _context = context;
            _newUsers = new List<TelegramBotUser>();
            _config = proxyConfig.Value;
            _cts = new CancellationTokenSource();
        }

        public void Connect(string accessToken, string name)
        {
            if (accessToken != null)
            {
                _accessToken = accessToken;
                _botName = name;
                _logger.LogInformation($"Connected TelegramBot {_botName} with token: {_accessToken}");

                _listeningBot = new CustomTelegramBot(accessToken, _config);
                _messageBot = new CustomTelegramBot(accessToken, _config);
            }
            else
            {
                _logger.LogError("Connect to telegramBot failed because accessToken not found");
            }
        }

        private async Task<Boolean> CheckConnect(CustomTelegramBot bot)
        {
            try
            {
                var me = await bot.MakeRequestAsync(new GetMe());
                if (me != null)
                {
                    _logger.LogTrace("{0} (@{1}) connected (proxy: {2})!", me.FirstName, me.Username, bot.Proxy);
                    return true;
                }

                _logger.LogError($"Bot connected failed! (proxy: {bot.Proxy})");
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError($"Bot connected failed, exception! (proxy: {bot.Proxy})", e);
                return false;
            }
        }

        public void Start()
        {
            _logger.LogInformation("TelegramBot start listening");
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await Listening();
                }
                finally
                {
                    _logger.LogInformation("Stop listening");
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _logger.LogInformation("TelegramBot stop listening");
            }
        }

        private async Task Listening()
        {
            long offset = 0;
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(100);
                    if (false == await CheckConnect(_listeningBot))
                    {
                        _listeningBot.ProxyIndex += 1;
                        await Task.Delay(500);

                        continue;
                    }

                    var updates = await _listeningBot.MakeRequestAsync(new GetUpdates() { Offset = offset });
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
                catch (Exception e)
                {
                    _logger.LogError($"Error occured while listening telegram (proxy: {_listeningBot.Proxy})", e);
                    _listeningBot.ProxyIndex += 1;
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
                await _listeningBot.MakeRequestAsync(req);
                return;
            }
            var newUser = _newUsers.FirstOrDefault(_ => _.TelegramName == chat.Username);
            if (newUser == null)
            {
                var req = new SendMessage(chat.Id, $"Hello {chat.FirstName} {chat.LastName}. Whats you name in gitlab?");
                await _listeningBot.MakeRequestAsync(req);
                _newUsers.Add(new TelegramBotUser() { TelegramName = chat.Username, ChatId = chat.Id });
            }
            else if (string.IsNullOrEmpty(newUser.GitlabUserName))
            {
                newUser.GitlabUserName = message.Text;
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                _newUsers.Remove(newUser);
                var req = new SendMessage(chat.Id, $"Very well! Now you registered");
                await _listeningBot.MakeRequestAsync(req);
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
                await _listeningBot.MakeRequestAsync(req);
                _logger.LogInformation($"User deleted: Chat: {user.ChatId} TelegramName: {user.TelegramName} GitlabName: {user.GitlabUserName}");
            }
            else
            {
                var req = new SendMessage(chat.Id, $"You are not registered yet");
                await _listeningBot.MakeRequestAsync(req);
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
            await _listeningBot.MakeRequestAsync(reqAction);
        }

        public async Task SendMessage(IEnumerable<TelegramBotUser> users, string message)
        {
            try
            {
                var connected = false;
                var maxRetry = 50;
                for(var idx = 0; idx < maxRetry; ++idx)
                {
                    connected = await CheckConnect(_messageBot);
                    if(false == connected)
                    {
                        _messageBot.ProxyIndex += 1;
                        _logger.LogInformation($"Tried to connect: {idx}/{maxRetry}");
                        await Task.Delay(100);
                    }
                    else
                    {
                        break;
                    }
                }
                if(connected == false)
                {
                    _logger.LogError($"Can't connect to send message: '{message}'");
                    return;
                }

                foreach (var user in users)
                {
                    _logger.LogInformation($"Send message: '{message}' chat: {user.ChatId} TelegramUser: {user.TelegramName}");
                    await _messageBot.MakeRequestAsync(new SendMessage(user.ChatId, message));
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in sending message from bot (proxy: {_messageBot.Proxy})", e);
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
        private readonly ProxiesConfig _config;

        private CustomTelegramBot _listeningBot;
        private CustomTelegramBot _messageBot;
        private CancellationTokenSource _cts;
        private string _botName;
        private string _accessToken;
    }
}
