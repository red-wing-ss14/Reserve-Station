using System;
using Content.Shared._RW.Jukebox;
using Content.Shared.Popups;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client._RW.Jukebox;

public sealed class RwTapeCreatorBUI : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private RwTapeCreatorMenu? _window;
    private RwTapeCreatorSystem? _tapeCreatorSystem;

    public RwTapeCreatorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!_entityManager.TryGetComponent<RwTapeCreatorComponent>(Owner, out var tapeCreatorComponent))
        {
            _entityManager.System<SharedPopupSystem>()
                .PopupEntity(Loc.GetString("amour-tape-creator-missing-component"), Owner);
            Close();
            return;
        }

        _window = new RwTapeCreatorMenu(tapeCreatorComponent);
        _window.OpenCentered();
        _window.OnClose += Close;

        _tapeCreatorSystem = _entityManager.System<RwTapeCreatorSystem>();
        _tapeCreatorSystem.UploadResponseReceived += OnUploadResponse;
    }

    private void OnUploadResponse(NetEntity tapeCreatorUid, bool success, string? message)
    {
        if (_window == null)
            return;

        if (tapeCreatorUid != _entityManager.GetNetEntity(Owner))
            return;

        _window.HandleUploadResponse(success, message);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        if (_tapeCreatorSystem != null)
            _tapeCreatorSystem.UploadResponseReceived -= OnUploadResponse;

        _window?.Dispose();
    }
}
