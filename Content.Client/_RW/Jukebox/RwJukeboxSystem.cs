using System;
using System.Collections.Generic;
using Content.Shared._RW;
using Content.Shared._RW.Jukebox;
using Content.Shared.GameTicking;
using Content.Shared.Item;
using Content.Shared.Physics;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Client._RW.Jukebox;

public sealed class RwJukeboxSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAudioManager _clydeAudio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;

    private const CollisionGroup CollisionMask = CollisionGroup.Impassable;
    private const float PlaybackDriftTolerance = 1.5f;

    private readonly Dictionary<RwJukeboxComponent, JukeboxAudio> _playingJukeboxes = new();

    private float _jukeboxVolume;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(RwCVars.JukeboxVolume, JukeboxVolumeChanged, true);

        SubscribeLocalEvent<RwJukeboxComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeNetworkEvent<RwJukeboxStopPlaying>(OnStopPlaying);
    }

    private void JukeboxVolumeChanged(float volume)
    {
        _jukeboxVolume = volume;

        foreach (var (component, audio) in _playingJukeboxes)
        {
            SetStreamVolume(component, audio);
        }
    }
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanUp();
    }

    private void OnComponentRemoved(EntityUid uid, RwJukeboxComponent component, ComponentRemove args)
    {
        if (!_playingJukeboxes.TryGetValue(component, out var playingData))
            return;

        playingData.PlayingStream.StopPlaying();
        playingData.PlayingStream.Dispose();
        _playingJukeboxes.Remove(component);
    }

    private void OnStopPlaying(RwJukeboxStopPlaying ev)
    {
        if (ev.JukeboxUid == null)
            return;

        var entity = GetEntity(ev.JukeboxUid.Value);
        if (!TryComp<RwJukeboxComponent>(entity, out var jukeboxComponent))
            return;

        if (!_playingJukeboxes.TryGetValue(jukeboxComponent, out var jukeboxAudio))
            return;

        jukeboxAudio.PlayingStream.StopPlaying();
        jukeboxAudio.PlayingStream.Dispose();
        _playingJukeboxes.Remove(jukeboxComponent);
    }

    public void RequestSongToPlay(EntityUid jukebox, RwJukeboxComponent component, RwJukeboxSong jukeboxSong)
    {
        if (jukeboxSong.SongPath == null)
            return;

        if (!_resource.TryGetResource<AudioResource>(jukeboxSong.SongPath.Value, out var songResource))
            return;

        RaiseNetworkEvent(new RwJukeboxRequestSongPlay
        {
            Jukebox = GetNetEntity(jukebox),
            SongName = jukeboxSong.SongName,
            SongPath = jukeboxSong.SongPath,
            SongDuration = (float) songResource.AudioStream.Length.TotalSeconds
        });
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_playerManager.LocalEntity == null)
        {
            CleanUp();
            return;
        }

        ProcessJukeboxes();
    }

    private void ProcessJukeboxes()
    {
        var player = _playerManager.LocalEntity!.Value;
        var playerXform = Transform(player);
        var query = EntityQueryEnumerator<RwJukeboxComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var jukeboxComponent, out var jukeboxXform))
        {
            if (jukeboxXform.MapID != playerXform.MapID ||
                (_transform.GetWorldPosition(uid) - _transform.GetWorldPosition(player)).Length() > jukeboxComponent.MaxAudioRange)
            {
                if (_playingJukeboxes.Remove(jukeboxComponent, out var stream))
                {
                    stream.PlayingStream.StopPlaying();
                    stream.PlayingStream.Dispose();
                    SetBarsLayerVisible(uid, false);
                }
                continue;
            }

            if (_playingJukeboxes.TryGetValue(jukeboxComponent, out var jukeboxAudio))
            {
                if (jukeboxComponent.Paused)
                {
                    if (jukeboxAudio.PlayingStream.Playing)
                        jukeboxAudio.PlayingStream.Pause();
                    continue;
                }

                if (!jukeboxAudio.PlayingStream.Playing)
                {
                    var songData = jukeboxComponent.PlayingSongData;
                    var sameSong = songData != null && jukeboxAudio.SongData.SongPath == songData.SongPath;
                    var notFinished = songData != null
                        && songData.PlaybackPosition + PlaybackDriftTolerance < songData.ActualSongLengthSeconds;

                    if (sameSong && notFinished)
                    {
                        jukeboxAudio.PlayingStream.StartPlaying();
                    }
                    else
                    {
                        HandleDoneStream(uid, player, jukeboxAudio, jukeboxComponent);
                        continue;
                    }
                }

                if (jukeboxAudio.SongData.SongPath != jukeboxComponent.PlayingSongData?.SongPath)
                {
                    HandleSongChanged(uid, player, jukeboxAudio, jukeboxComponent);
                    continue;
                }

                SyncPlaybackPosition(jukeboxComponent, jukeboxAudio);
                SetStreamVolume(jukeboxComponent, jukeboxAudio);
                SetRolloffAndOcclusion(player, uid, jukeboxComponent, jukeboxAudio);
                SetPosition(uid, jukeboxAudio);
            }
            else
            {
                if (jukeboxComponent.PlayingSongData == null)
                {
                    SetBarsLayerVisible(uid, false);
                    continue;
                }

                var stream = TryCreateStream(uid, player, jukeboxComponent);
                if (stream == null)
                    continue;

                _playingJukeboxes.Add(jukeboxComponent, stream);
                SetBarsLayerVisible(uid, true);
            }
        }
    }

    private void SetPosition(EntityUid jukebox, JukeboxAudio jukeboxAudio)
    {
        jukeboxAudio.PlayingStream.Position = _transform.GetWorldPosition(jukebox);
    }

    private void SetRolloffAndOcclusion(EntityUid player, EntityUid jukebox, RwJukeboxComponent jukeboxComponent, JukeboxAudio jukeboxAudio)
    {
        var jukeboxPos = _transform.GetWorldPosition(jukebox);
        var playerPos = _transform.GetWorldPosition(player);
        var sourceRelative = playerPos - jukeboxPos;
        var occlusion = 0f;

        if (sourceRelative.Length() > 0)
        {
            occlusion = _physicsSystem.IntersectRayPenetration(_transform.GetMapCoordinates(jukebox).MapId,
                new CollisionRay(jukeboxPos, sourceRelative.Normalized(), (int) CollisionMask),
                sourceRelative.Length(), jukebox) * 3f;
        }

        jukeboxAudio.PlayingStream.Occlusion = occlusion;
        jukeboxAudio.PlayingStream.RolloffFactor = (jukeboxPos - playerPos).Length() * jukeboxComponent.RolloffFactor;
    }

    private void SetStreamVolume(RwJukeboxComponent jukeboxComponent, JukeboxAudio jukeboxAudio)
    {
        var jukeboxGain = Math.Clamp(jukeboxComponent.Volume, 0f, 1f);
        jukeboxAudio.PlayingStream.Gain = MathF.Max(0f, _jukeboxVolume) * jukeboxGain;
    }

    private void SyncPlaybackPosition(RwJukeboxComponent jukeboxComponent, JukeboxAudio jukeboxAudio)
    {
        if (jukeboxComponent.PlayingSongData == null)
            return;

        var playbackPosition = jukeboxComponent.PlayingSongData.PlaybackPosition;
        if (MathF.Abs(jukeboxAudio.PlayingStream.PlaybackPosition - playbackPosition) <= PlaybackDriftTolerance)
            return;

        jukeboxAudio.PlayingStream.PlaybackPosition = playbackPosition;
    }

    private void HandleSongChanged(EntityUid jukebox, EntityUid player, JukeboxAudio jukeboxAudio, RwJukeboxComponent jukeboxComponent)
    {
        jukeboxAudio.PlayingStream.StopPlaying();
        jukeboxAudio.PlayingStream.Dispose();

        if (jukeboxComponent.PlayingSongData != null)
        {
            var newStream = TryCreateStream(jukebox, player, jukeboxComponent);
            if (newStream == null)
            {
                _playingJukeboxes.Remove(jukeboxComponent);
                SetBarsLayerVisible(jukebox, false);
            }
            else
            {
                _playingJukeboxes[jukeboxComponent] = newStream;
                SetBarsLayerVisible(jukebox, true);
            }
        }
        else
        {
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukebox, false);
        }
    }

    private void HandleDoneStream(EntityUid jukebox, EntityUid player, JukeboxAudio jukeboxAudio, RwJukeboxComponent jukeboxComponent)
    {
        if (!jukeboxComponent.Playing || jukeboxComponent.PlayingSongData == null)
        {
            jukeboxAudio.PlayingStream.StopPlaying();
            jukeboxAudio.PlayingStream.Dispose();
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukebox, false);
            return;
        }

        var newStream = TryCreateStream(jukebox, player, jukeboxComponent);
        if (newStream == null)
        {
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukebox, false);
        }
        else
        {
            _playingJukeboxes[jukeboxComponent] = newStream;
            SetBarsLayerVisible(jukebox, true);
        }
    }

    private JukeboxAudio? TryCreateStream(EntityUid jukebox, EntityUid player, RwJukeboxComponent jukeboxComponent)
    {
        if (jukeboxComponent.PlayingSongData?.SongPath == null)
            return null;

        if (!_resource.TryGetResource<AudioResource>(jukeboxComponent.PlayingSongData.SongPath.Value, out var audio))
            return null;

        if (audio.AudioStream.Length.TotalSeconds < jukeboxComponent.PlayingSongData.PlaybackPosition)
            return null;

        var playingStream = _clydeAudio.CreateAudioSource(audio.AudioStream);
        if (playingStream == null)
            return null;

        playingStream.Gain = MathF.Max(0f, _jukeboxVolume) * Math.Clamp(jukeboxComponent.Volume, 0f, 1f);
        playingStream.PlaybackPosition = jukeboxComponent.PlayingSongData.PlaybackPosition;
        playingStream.Position = _transform.GetWorldPosition(jukebox);

        var jukeboxAudio = new JukeboxAudio(playingStream, audio, jukeboxComponent.PlayingSongData);
        SetRolloffAndOcclusion(player, jukebox, jukeboxComponent, jukeboxAudio);
        playingStream.StartPlaying();

        return jukeboxAudio;
    }

    private void SetBarsLayerVisible(EntityUid jukebox, bool visible)
    {
        if (!TryComp<SpriteComponent>(jukebox, out var sprite))
            return;

        if (sprite.LayerMapTryGet("bars", out var layer))
            sprite.LayerSetVisible(layer, visible);

        if (sprite.LayerMapTryGet("light", out var lightLayer))
            sprite.LayerSetVisible(lightLayer, visible);

        if (TryComp<ItemComponent>(jukebox, out var item))
        {
            if (visible)
            {
                _itemSystem.SetHeldPrefix(jukebox, null, false, item);
            }
            else
            {
                _itemSystem.SetHeldPrefix(jukebox, "off", false, item);
            }
        }
    }

    private sealed class JukeboxAudio(IAudioSource playingStream, AudioResource audioStream, AmourPlayingSongData songData)
    {
        public AmourPlayingSongData SongData { get; } = songData;
        public IAudioSource PlayingStream { get; } = playingStream;
        public AudioResource AudioStream { get; } = audioStream;
    }

    private void CleanUp()
    {
        foreach (var playingJukebox in _playingJukeboxes.Values)
        {
            playingJukebox.PlayingStream.StopPlaying();
            playingJukebox.PlayingStream.Dispose();
        }
        _playingJukeboxes.Clear();
    }
}
