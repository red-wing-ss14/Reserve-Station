using Content.Shared._RW;
using Content.Shared._RW.TTS;
using Content.Shared.GameTicking;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client._RW.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private ResPath _prefix;

    /// <summary>
    /// Reducing the volume of the TTS when whispering. Will be converted to logarithm.
    /// </summary>
    private const float WhisperFade = 4f;

    /// <summary>
    /// Voice range in world units (same as server ChatSystem.VoiceRange).
    /// </summary>
    private const float VoiceRange = 10f;

    /// <summary>
    /// Whisper muffled range in world units (same as server ChatSystem.WhisperMuffledRange).
    /// </summary>
    private const float WhisperMuffledRange = 5f;

    private float _volume = 0.0f;
    private ulong _fileIdx = 0;
    private static ulong _shareIdx = 0;

    private bool _ttsEnabled;

    public override void Initialize()
    {
        _prefix = ResPath.Root / $"TTS{_shareIdx++}";
        _sawmill = Logger.GetSawmill("tts");
        _res.AddRoot(_prefix, _contentRoot);
        _cfg.OnValueChanged(WhiteCVars.TtsVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(WhiteCVars.VoiceType, OnVoiceTypeChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _contentRoot.Clear();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(WhiteCVars.TtsVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(WhiteCVars.VoiceType, OnVoiceTypeChanged);
        _contentRoot.Dispose();
    }

    public void RequestGlobalTTS(VoiceRequestType text, string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnVoiceTypeChanged(CharacterVoiceType voiceType)
    {
        _ttsEnabled = voiceType == CharacterVoiceType.TTS;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        if (!_ttsEnabled)
            return;

        _sawmill.Verbose($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        var audioResource = new AudioResource();
        audioResource.Load(IoCManager.Instance!, _prefix / filePath);

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(ev.IsWhisper))
            .WithMaxDistance(AdjustDistance(ev.IsWhisper));

        if (ev.SourceUid != null)
        {
            var sourceUid = GetEntity(ev.SourceUid.Value);
            if(sourceUid.IsValid())
                _audio.PlayEntity(audioResource.AudioStream, sourceUid, null, audioParams);
        }
        else
        {
            _audio.PlayGlobal(audioResource.AudioStream, null, audioParams);
        }

        _contentRoot.RemoveFile(filePath);
    }

    private float AdjustVolume(bool isWhisper)
    {
        var volume = SharedAudioSystem.GainToVolume(_volume);

        if (isWhisper)
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);

        return volume;
    }

    private float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? WhisperMuffledRange : VoiceRange;
    }
}
