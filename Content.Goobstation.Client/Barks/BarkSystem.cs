using Content.Goobstation.Common.Barks;
using Content.Shared._RW;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Goobstation.Common.CCVar;

namespace Content.Goobstation.Client.Barks;

public sealed class BarkSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudio = default!;

    private readonly Dictionary<NetEntity, EntityUid> _playingSounds = new();
    private static readonly char[] Characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

    private readonly List<ActiveBark> _activeBarks = new();

    private bool _barksEnabled = true; // RW - TTS toggle

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(WhiteCVars.VoiceType, OnVoiceTypeChanged, true); // RW - TTS toggle
        SubscribeNetworkEvent<PlayBarkEvent>(OnPlayBark);
        SubscribeLocalEvent<PreviewBarkEvent>(OnPreviewBark);
    }

    // RW - TTS toggle Start
    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(WhiteCVars.VoiceType, OnVoiceTypeChanged);
    }

    private void OnVoiceTypeChanged(CharacterVoiceType voiceType)
    {
        _barksEnabled = voiceType == CharacterVoiceType.Barks;
    }
    // RW - TTS toggle End

    public void OnPreviewBark(PreviewBarkEvent ev)
    {
        if (!_prototypeManager.TryIndex<BarkPrototype>(ev.BarkProtoID, out var proto))
            return;

        var messageLength = _random.Next(5, 20);
        var message = new char[messageLength];
        for (var i = 0; i < messageLength; i++)
        {
            message[i] = _random.Pick(Characters);
        }
        PlayBark(null, new string(message), false, proto, ev.BarkSettings); // RW edit: bug-fixes #12
    }

    private void OnPlayBark(PlayBarkEvent ev)
    {
        if (!_barksEnabled) // RW - TTS toggle
            return;

        var sourceEntity = GetEntity(ev.SourceUid);
        if (!TryComp<SpeechSynthesisComponent>(sourceEntity, out var comp)
            || comp.VoicePrototypeId is null
            || !_prototypeManager.TryIndex<BarkPrototype>(comp.VoicePrototypeId, out var proto))
            return;

        PlayBark(sourceEntity, ev.Message, ev.Whisper, proto, comp.BarkSettings); // RW edit: bug-fixes #12
    }

    // RW edit start: bug-fixes #12
    private void PlayBark(EntityUid? source, string message, bool whisper, BarkPrototype proto, BarkPercentageApplyData settings)
    {
        if (proto.SoundCollection is null)
            return;

        if (message.Length > 50)
            message = message[..50];

        var volume = GetVolume(whisper, proto);
        if (volume <= -20f)
            return;

        var upperCount = 0;
        foreach (var c in message)
            if (char.IsUpper(c))
                upperCount++;

        if (upperCount > message.Length / 2
            || message.EndsWith("!!"))
            volume += 5;

        var messageLength = message.Length;
        var totalDuration = Math.Max(0.1f, messageLength * 0.05f);
        var pause = BarkSettingsUtility.GetPause(settings);
        const float defaultPause = 0.085f;
        var soundInterval = 0.08f / proto.Frequency * (pause / defaultPause);
        var soundCount = (int) Math.Max(1, totalDuration / soundInterval);

        var activeBark = new ActiveBark
        {
            Source = source,
            IsPreview = source == null,
            Message = message,
            Prototype = proto,
            Settings = settings,
            Volume = volume,
            TotalSounds = soundCount,
            SoundInterval = soundInterval,
            NextSound = _timing.CurTime
        };

        _activeBarks.Add(activeBark);
    }
    // RW edit end: bug-fixes #12

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        for (var i = _activeBarks.Count - 1; i >= 0; i--)
        {
            var bark = _activeBarks[i];

            if (bark.NextSound > _timing.CurTime)
                continue;

            if (!bark.IsPreview && TerminatingOrDeleted(bark.Source!.Value))
            {
                _activeBarks.RemoveAt(i);
                continue;
            }

            var character = bark.Message[bark.CurrentSound % bark.Message.Length];
            if (character != ' ' && character != '-')
                PlaySound(bark, character);

            bark.CurrentSound++;
            bark.NextSound += TimeSpan.FromSeconds(bark.SoundInterval);

            if (bark.CurrentSound >= bark.TotalSounds)
                _activeBarks.RemoveAt(i);
        }
    }

    private void PlaySound(ActiveBark bark, char character)
    {
        var proto = bark.Prototype;
        var sound = _sharedAudio.ResolveSound(proto.SoundCollection!);
        var audioParams = proto.SoundCollection!.Params;

        var pitchMult = BarkSettingsUtility.GetPitch(bark.Settings);
        var pitchVariance = BarkSettingsUtility.GetPitchVariance(bark.Settings);
        var minPitch = proto.MinPitch * pitchMult - pitchVariance;
        var maxPitch = proto.MaxPitch * pitchMult + pitchVariance;
        minPitch = MathF.Max(0.1f, minPitch);
        maxPitch = MathF.Max(minPitch, maxPitch);

        if (proto.Predictable)
        {
            var hashCode = character.GetHashCode();

            if (sound is ResolvedCollectionSpecifier collection && collection.Collection != null)
            {
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(collection.Collection);
                var index = hashCode % soundCollection.PickFiles.Count;
                sound = new ResolvedCollectionSpecifier(collection.Collection, index);
            }

            var minPitchInt = (int) (minPitch * 100);
            var maxPitchInt = (int) (maxPitch * 100);
            var pitchRangeInt = maxPitchInt - minPitchInt;
            if (pitchRangeInt != 0)
            {
                var predictablePitchInt = hashCode % pitchRangeInt + minPitchInt;
                var predictablePitch = predictablePitchInt / 100f;
                audioParams = audioParams.WithPitchScale(predictablePitch);
            }
            else
            {
                audioParams = audioParams.WithPitchScale(minPitch);
            }
        }
        else
        {
            audioParams = audioParams.WithPitchScale(_random.NextFloat(minPitch, maxPitch));
        }

        audioParams = audioParams.WithVolume(bark.Volume);

        var filter = Filter.Local();
        var soundEntity = bark.IsPreview
            ? _sharedAudio.PlayGlobal(sound, filter, false, audioParams)
            : _sharedAudio.PlayEntity(sound, filter, bark.Source!.Value, false, audioParams);

        if (!bark.IsPreview && proto.Stop)
        {
            if (_playingSounds.TryGetValue(GetNetEntity(bark.Source!.Value), out var playing))
                _sharedAudio.Stop(playing);
        }

        if (!bark.IsPreview && soundEntity is not null)
            _playingSounds[GetNetEntity(bark.Source!.Value)] = soundEntity.Value.Entity;
    }

    private float GetVolume(bool whisper, BarkPrototype proto)
    {
        var volume = proto.Volume;

        if (whisper)
            volume = 0.05f + (volume - 0.05f) * 0.25f;

        var barksVolume = _cfg.GetCVar(GoobCVars.BarksVolume);
        volume *= barksVolume / 3f;

        return SharedAudioSystem.GainToVolume(volume);
    }

    private sealed class ActiveBark
    {
        public EntityUid? Source;
        public bool IsPreview;
        public string Message = string.Empty;
        public BarkPrototype Prototype = default!;
        public BarkPercentageApplyData Settings = BarkPercentageApplyData.Default;
        public float Volume;

        public int TotalSounds;
        public int CurrentSound;
        public float SoundInterval;
        public TimeSpan NextSound;
    }
}
