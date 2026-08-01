// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using JukeboxComponent = Content.Shared.Audio.Jukebox.JukeboxComponent;

namespace Content.Server.Audio.Jukebox;


public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!; // RW

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetVolumeMessage>(OnJukeboxSetVolume); // RW
        SubscribeLocalEvent<JukeboxComponent, JukeboxToggleLoopMessage>(OnJukeboxToggleLoop); // RW
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component, ref JukeboxPlayingMessage args)
    {
        // RW-Edit-Start: Fix PVS error with invalid AudioStream entity reference
        // Clean up invalid AudioStream reference
        if (component.AudioStream != null && !Exists(component.AudioStream))
        {
            component.AudioStream = null;
        }

        if (component.AudioStream != null && Exists(component.AudioStream))
        // RW-Edit-End
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);

            // RW-Start
            if (component.PlaybackStartTime == null && component.CurrentPlaybackOffset > 0)
            {
                Audio.SetPlaybackPosition(component.AudioStream, component.CurrentPlaybackOffset);
            }
            component.PlaybackStartTime = _gameTiming.CurTime;
            Dirty(uid, component);
            // RW-End
        }
        else
        {
            component.AudioStream = Audio.Stop(component.AudioStream);

            if (string.IsNullOrEmpty(component.SelectedSongId) ||
                !_protoManager.Resolve(component.SelectedSongId, out var jukeboxProto))
            {
                return;
            }

            component.AudioStream = Audio.PlayPvs(jukeboxProto.Path, uid, AudioParams.Default.WithMaxDistance(10f).WithVolume(MapToRange(component.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume)))?.Entity; // RW-Edit
            // RW-Start
            component.PlaybackStartTime = _gameTiming.CurTime;
            component.CurrentPlaybackOffset = 0f;
            // RW-End
            Dirty(uid, component);
        }
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        // RW-Edit-Start: Fix PVS error with invalid AudioStream entity reference
        // Clean up invalid AudioStream reference
        if (ent.Comp.AudioStream != null && !Exists(ent.Comp.AudioStream))
        {
            ent.Comp.AudioStream = null;
            Dirty(ent);
            return;
        }

        if (ent.Comp.AudioStream == null)
            return;
        // RW-Edit-End

        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);

        // RW-Start
        if (!ent.Comp.PlaybackStartTime.HasValue)
            return;

        var elapsed = (float)(_gameTiming.CurTime - ent.Comp.PlaybackStartTime.Value).TotalSeconds;
        ent.Comp.CurrentPlaybackOffset += elapsed;
        ent.Comp.PlaybackStartTime = null;
        Dirty(ent);
        // RW-End
    }

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        if (!TryComp(args.Actor, out ActorComponent? actorComp))
            return;

        // RW-Edit-Start: Fix PVS error with invalid AudioStream entity reference
        // Clean up invalid AudioStream reference
        if (component.AudioStream != null && !Exists(component.AudioStream))
        {
            component.AudioStream = null;
            Dirty(uid, component);
            return;
        }

        if (component.AudioStream == null)
            return;
        // RW-Edit-End

        var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
        var newPosition = args.SongTime + offset; // RW
        Audio.SetPlaybackPosition(component.AudioStream, newPosition); // RW-Edit

        // RW-Start
        component.CurrentPlaybackOffset = newPosition;
        component.PlaybackStartTime = _gameTiming.CurTime;
        Dirty(uid, component);
        // RW-End
    }

    // RW-Start
    private void OnJukeboxSetVolume(EntityUid uid, JukeboxComponent component, JukeboxSetVolumeMessage args)
    {
        SetJukeboxVolume(uid, component, args.Volume);

        // RW-Edit-Start: Fix PVS error with invalid AudioStream entity reference
        // Clean up invalid AudioStream reference
        if (component.AudioStream != null && !Exists(component.AudioStream))
        {
            component.AudioStream = null;
            Dirty(uid, component);
            return;
        }

        if (component.AudioStream == null || !TryComp<AudioComponent>(component.AudioStream, out _))
            return;
        // RW-Edit-End

        Audio.SetVolume(component.AudioStream, MapToRange(args.Volume, component.MinSlider, component.MaxSlider, component.MinVolume, component.MaxVolume));
    }

    private void OnJukeboxToggleLoop(EntityUid uid, JukeboxComponent component, JukeboxToggleLoopMessage args)
    {
        ToggleLoop(uid, component);
    }
    // RW-End

    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Stop(entity);
    }

    private void Stop(Entity<JukeboxComponent> entity)
    {
        // RW-Edit-Start: Fix PVS error with invalid AudioStream entity reference
        // Clean up invalid AudioStream reference
        if (entity.Comp.AudioStream != null && !Exists(entity.Comp.AudioStream))
        {
            entity.Comp.AudioStream = null;
        }

        if (entity.Comp.AudioStream != null)
        {
            Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
        }
        // RW-Edit-End

        // RW-Start
        entity.Comp.CurrentPlaybackOffset = 0f;
        entity.Comp.PlaybackStartTime = null;
        // RW-End
        Dirty(entity);
    }

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        // RW-Edit-Start: Fix PVS error with invalid AudioStream entity reference
        // Clean up invalid AudioStream reference
        if (component.AudioStream != null && !Exists(component.AudioStream))
        {
            component.AudioStream = null;
        }
        // RW-Edit-End

        if (!Audio.IsPlaying(component.AudioStream))
        {
            component.SelectedSongId = args.SongId;
            DirectSetVisualState(uid, JukeboxVisualState.Select);
            component.Selecting = true;
            component.AudioStream = Audio.Stop(component.AudioStream);
        }

        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Selecting)
            {
                comp.SelectAccumulator += frameTime;
                if (comp.SelectAccumulator >= 0.5f)
                {
                    comp.SelectAccumulator = 0f;
                    comp.Selecting = false;

                    TryUpdateVisualState(uid, comp);
                }
            }

            // RW-Start
            // RW-Edit-Start: Fix PVS error with invalid AudioStream entity reference
            // Clean up invalid AudioStream reference
            if (comp.AudioStream != null && !Exists(comp.AudioStream))
            {
                comp.AudioStream = null;
                comp.PlaybackStartTime = null;
                comp.CurrentPlaybackOffset = 0f;
                Dirty(uid, comp);
                continue;
            }

            if (!comp.LoopEnabled || !comp.PlaybackStartTime.HasValue || comp.AudioStream == null ||
                !TryComp<AudioComponent>(comp.AudioStream, out var audioComp))
                continue;
            // RW-Edit-End

            var audioLength = Audio.GetAudioLength(audioComp.FileName);
            var elapsed = (float)(_gameTiming.CurTime - comp.PlaybackStartTime.Value).TotalSeconds;
            var currentPosition = comp.CurrentPlaybackOffset + elapsed;

            if (!(currentPosition >= audioLength.TotalSeconds))
                continue;

            // Restart track
            Audio.SetPlaybackPosition(comp.AudioStream, 0f);
            Audio.SetState(comp.AudioStream, AudioState.Playing);
            comp.CurrentPlaybackOffset = 0f;
            comp.PlaybackStartTime = _gameTiming.CurTime;
            Dirty(uid, comp);
            // RW-End
        }
    }

    // RW-Start
    private void SetJukeboxVolume(EntityUid uid, JukeboxComponent component, float volume)
    {
        component.Volume = volume;
        Dirty(uid, component);
    }

    private void ToggleLoop(EntityUid uid, JukeboxComponent component)
    {
        component.LoopEnabled = !component.LoopEnabled;
        Dirty(uid, component);
    }
    // RW-End

    private void OnComponentShutdown(EntityUid uid, JukeboxComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(EntityUid uid, JukeboxComponent? jukeboxComponent = null)
    {
        if (!Resolve(uid, ref jukeboxComponent))
            return;

        var finalState = JukeboxVisualState.On;

        if (!this.IsPowered(uid, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, finalState);
    }
}
