using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Shared._RW.CCVar;
using Content.Shared.CCVar;
using Discord;
using Discord.WebSocket;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server._RW.Discord;

public sealed class DiscordOocBridgeService : IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private DiscordSocketClient? _client;
    private ulong _channelId;
    private bool _isRunning;
    private ISawmill _sawmill = default!;
    private CancellationTokenSource? _statusUpdateCts;

    private readonly ConcurrentDictionary<ulong, Queue<DateTime>> _discordRateLimit = new();
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _gameRateLimit = new();
    private const int MaxMessagesPerWindow = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromSeconds(10);

    public void PostInject()
    {
        _sawmill = Logger.GetSawmill("discord-ooc");
    }

    public async Task StartAsync()
    {
        var token = _config.GetCVar(RwCCVars.DiscordOocBotToken);
        _channelId = _config.GetCVar(RwCCVars.DiscordOocChannelId);

        if (string.IsNullOrWhiteSpace(token) || _channelId == 0)
        {
            return;
        }

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        });

        _client.Log += LogAsync;
        _client.MessageReceived += OnDiscordMessageAsync;
        _client.Ready += OnReadyAsync;

        try
        {
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            _isRunning = true;

            _statusUpdateCts = new CancellationTokenSource();
            _ = UpdateStatusLoop(_statusUpdateCts.Token);
        }
        catch (Exception ex)
        {
            _sawmill.Error("Failed to start Discord OOC bridge: {0}", ex);
        }
    }

    public async Task StopAsync()
    {
        if (_client == null || !_isRunning)
            return;

        _isRunning = false;

        _statusUpdateCts?.Cancel();
        _statusUpdateCts?.Dispose();
        _statusUpdateCts = null;
        
        await _client.StopAsync();
        await _client.DisposeAsync();
        _client = null;
    }

    public async Task SendToDiscordAsync(string playerName, string message)
    {
        if (_client == null || !_isRunning || _channelId == 0)
            return;

        if (!CheckGameRateLimit(playerName))
        {
            return;
        }

        try
        {
            if (_client.GetChannel(_channelId) is not IMessageChannel channel)
            {
                _sawmill.Warning($"Channel {_channelId} not found");
                return;
            }

            var sanitizedMessage = SanitizeMessage(message);
            var formattedMessage = $"**{EscapeMarkdown(playerName)}:** {sanitizedMessage}";
            await channel.SendMessageAsync(formattedMessage);
        }
        catch (Exception ex)
        {
            _sawmill.Error("Failed to send message to Discord: {0}", ex);
        }
    }

    private Task LogAsync(global::Discord.LogMessage log)
    {
        var severity = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Error,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Info,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Info
        };

        _sawmill.Log(severity, $"{log.Source}: {log.Message} {log.Exception}");
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        return Task.CompletedTask;
    }

    private async Task OnDiscordMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || message.Channel.Id != _channelId)
            return;

        if (!CheckDiscordRateLimit(message.Author.Id))
        {
            await message.Channel.SendMessageAsync($"{message.Author.Mention}, you're sending messages too quickly!");
            return;
        }

        var content = message.CleanContent;
        if (string.IsNullOrWhiteSpace(content))
            return;

        if (content.Length > 500)
            content = content[..500] + "...";

        var sender = message.Author.Username;

        _taskManager.RunOnMainThread(() => _chatManager.SendHookOOC(sender, content, isDiscordBridge: true));
    }

    private bool CheckDiscordRateLimit(ulong userId)
    {
        var now = DateTime.UtcNow;
        var queue = _discordRateLimit.GetOrAdd(userId, _ => new Queue<DateTime>());

        lock (queue)
        {
            while (queue.Count > 0 && (now - queue.Peek()) > RateLimitWindow)
                queue.Dequeue();

            if (queue.Count >= MaxMessagesPerWindow)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }

    private bool CheckGameRateLimit(string playerName)
    {
        var now = DateTime.UtcNow;
        var queue = _gameRateLimit.GetOrAdd(playerName, _ => new Queue<DateTime>());

        lock (queue)
        {
            while (queue.Count > 0 && (now - queue.Peek()) > RateLimitWindow)
                queue.Dequeue();

            if (queue.Count >= MaxMessagesPerWindow)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }

    private static string EscapeMarkdown(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace("|", "\\|")
            .Replace(">", "\\>");
    }

    private static string SanitizeMessage(string message)
    {
        message = message.Replace("@everyone", "забаньте меня");
        message = message.Replace("@here", "ㅤhere");

        message = System.Text.RegularExpressions.Regex.Replace(message, @"@([&!]?\d+|[a-zA-Z0-9_]+)", "ㅤ");

        message = System.Text.RegularExpressions.Regex.Replace(message, @"(https?://)", "ㅤ");

        message = EscapeMarkdown(message);
        
        return message;
    }

    private async Task UpdateStatusLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                
                if (_client?.CurrentUser == null)
                    continue;

                var playerCount = _playerManager.PlayerCount;
                var maxPlayers = _config.GetCVar(CCVars.SoftMaxPlayers);
                
                await _client.SetGameAsync($"Онлайн: {playerCount}/{maxPlayers}");
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _sawmill.Error("Failed to update Discord bot status: {0}", ex);
            }
        }
    }
}
