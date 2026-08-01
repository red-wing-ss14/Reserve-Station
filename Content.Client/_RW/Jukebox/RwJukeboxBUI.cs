using System;
using Content.Shared._RW.Jukebox;
using Content.Shared.Popups;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client._RW.Jukebox;

public sealed class RwJukeboxBUI : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private RwJukeboxMenu? _window;

    public RwJukeboxBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!_entityManager.TryGetComponent(Owner, out RwJukeboxComponent? jukeboxComponent))
        {
            _entityManager.System<SharedPopupSystem>()
                .PopupEntity(Loc.GetString("amour-jukebox-missing-component"), Owner);
            Close();
            return;
        }

        _window = new RwJukeboxMenu(Owner, jukeboxComponent);
        _window.PlayPausePressed += OnPlayPausePressed;
        _window.StopPressed += OnStopPressed;
        _window.EjectPressed += OnEjectPressed;
        _window.RepeatToggled += OnRepeatToggled;
        _window.SetPlaybackPosition += OnSetPlaybackPosition;
        _window.SetVolume += OnSetVolume;
        _window.OpenCentered();
        _window.OnClose += Close;
    }

    private void OnSetPlaybackPosition(float playbackPosition)
    {
        SendMessage(new RwJukeboxSetPlaybackPosition(playbackPosition));
    }

    private void OnSetVolume(float volume)
    {
        SendMessage(new RwJukeboxSetVolume(volume));
    }

    private void OnEjectPressed()
    {
        SendMessage(new RwJukeboxEjectRequest());
    }

    private void OnPlayPausePressed()
    {
        SendMessage(new RwJukeboxPlayPauseRequest());
    }
    private void OnStopPressed()
    {
        SendMessage(new RwJukeboxStopRequest());
    }

    private void OnRepeatToggled(bool newState)
    {
        SendMessage(new RwJukeboxRepeatToggled(newState));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }
}
