using Robust.Shared.Configuration;

namespace Content.Shared._RW;

/// <summary>
/// Type of voice synthesis for character speech.
/// </summary>
public enum CharacterVoiceType
{
    /// <summary>
    /// Use bark sounds (short sound effects).
    /// </summary>
    Barks = 0,

    /// <summary>
    /// Use Text-to-Speech synthesis.
    /// </summary>
    TTS = 1
}

[CVarDefs]
public sealed class WhiteCVars
{
    #region Aspects

    public static readonly CVarDef<bool> IsAspectsEnabled =
        CVarDef.Create("aspects.enabled", false, CVar.SERVERONLY);

    public static readonly CVarDef<double> AspectChance =
        CVarDef.Create("aspects.chance", 0.1d, CVar.SERVERONLY);

    #endregion

    #region Locale

    public static readonly CVarDef<string>
        ServerCulture = CVarDef.Create("white.culture", "ru-RU", CVar.REPLICATED | CVar.SERVER);

    #endregion

    #region OptionsMisc

    public static readonly CVarDef<bool> LogInChat =
        CVarDef.Create("white.log_in_chat", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    #endregion

    #region TTS

    /// <summary>
    /// Client-side voice type preference (Barks or TTS).
    /// </summary>
    public static readonly CVarDef<CharacterVoiceType> VoiceType =
        CVarDef.Create("voice.type", CharacterVoiceType.TTS, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If the TTS system is enabled or not.
    /// </summary>
    public static readonly CVarDef<bool> TtsEnabled =
        CVarDef.Create("tts.enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// URL of the TTS server API (e.g., "https://ntts.fdev.team/api/v1/tts").
    /// </summary>
    public static readonly CVarDef<string> TtsApiUrl =
        CVarDef.Create("tts.api_url", "https://ntts.fdev.team/api/v1/tts", CVar.SERVERONLY);

    /// <summary>
    /// Bearer token for TTS API authentication.
    /// </summary>
    public static readonly CVarDef<string> TtsApiToken =
        CVarDef.Create("tts.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Timeout for TTS API requests in seconds.
    /// </summary>
    public static readonly CVarDef<float> TtsApiTimeout =
        CVarDef.Create("tts.api_timeout", 10f, CVar.SERVERONLY);

    /// <summary>
    /// The volume of TTS playback.
    /// </summary>
    public static readonly CVarDef<float> TtsVolume =
        CVarDef.Create("tts.volume", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum number of cached TTS audio files.
    /// </summary>
    public static readonly CVarDef<int> TtsMaxCacheSize =
        CVarDef.Create("tts.max_cache_size", 200, CVar.SERVERONLY | CVar.ARCHIVE);

    #endregion
}
