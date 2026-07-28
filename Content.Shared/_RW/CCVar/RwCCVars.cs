using Robust.Shared.Configuration;

namespace Content.Shared._RW.CCVar;

[CVarDefs]
public sealed class RwCCVars
{
    /// <summary>
    ///     Number of gulag ore points required to reduce a temporary ban by one second.
    /// </summary>
    public static readonly CVarDef<double> GulagPointsToTimeRatio =
        CVarDef.Create("amour.gulag.points_to_time", 1d, CVar.SERVERONLY);

    /// <summary>
    ///     Automatically toggles OOC depending on current online player count.
    /// </summary>
    public static readonly CVarDef<bool> OocAutoToggleEnabled =
        CVarDef.Create("amour.ooc_auto_toggle_enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.NOTIFY);

    /// <summary>
    ///     OOC is enabled while online is at or below this value, and disabled above it.
    /// </summary>
    public static readonly CVarDef<int> OocAutoTogglePlayerThreshold =
        CVarDef.Create("amour.ooc_auto_toggle_player_threshold", 7, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord bot API for receiving OOC messages from Discord.
    /// </summary>
    public static readonly CVarDef<string> DiscordOocBotApiUrl =
        CVarDef.Create("discord.ooc_bot_api_url", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Password for Discord bot OOC API authentication.
    /// </summary>
    public static readonly CVarDef<string> DiscordOocBotApiPassword =
        CVarDef.Create("discord.ooc_bot_api_password", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     URL of the Discord webhook used to send in-game OOC messages to Discord.
    /// </summary>
    public static readonly CVarDef<string> DiscordOocWebhookUrl =
        CVarDef.Create("discord.ooc_webhook_url", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Discord bot token for embedded OOC bridge bot.
    /// </summary>
    public static readonly CVarDef<string> DiscordOocBotToken =
        CVarDef.Create("amour.discord_ooc_bot_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Discord channel ID for OOC bridge.
    /// </summary>
    public static readonly CVarDef<ulong> DiscordOocChannelId =
        CVarDef.Create("amour.discord_ooc_channel_id", 0UL, CVar.SERVERONLY);
}
