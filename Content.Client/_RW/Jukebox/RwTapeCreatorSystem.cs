using System;
using Content.Shared._RW.Jukebox;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._RW.Jukebox;

public sealed class RwTapeCreatorSystem : EntitySystem
{
    public event Action<NetEntity, bool, string?>? UploadResponseReceived;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RwTapeCreatorComponent, ComponentHandleState>(OnStateChanged);
        SubscribeLocalEvent<RwTapeComponent, ComponentHandleState>(OnTapeStateChanged);
        SubscribeNetworkEvent<RwJukeboxSongUploadResponse>(OnUploadResponse);
    }

    private void OnUploadResponse(RwJukeboxSongUploadResponse ev)
    {
        UploadResponseReceived?.Invoke(ev.TapeCreatorUid, ev.Success, ev.Message);
    }

    private void OnTapeStateChanged(EntityUid uid, RwTapeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RwTapeComponentState state)
            return;

        component.Songs = state.Songs;
    }

    private void OnStateChanged(EntityUid uid, RwTapeCreatorComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RwTapeCreatorComponentState state)
            return;

        component.Recording = state.Recording;
        component.CoinBalance = state.CoinBalance;
        component.InsertedTape = state.InsertedTape;

        SetTapeLayerVisible(uid, state.InsertedTape.HasValue);
    }

    private void SetTapeLayerVisible(EntityUid uid, bool visible)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.LayerMapTryGet("tape", out var layer))
            sprite.LayerSetVisible(layer, visible);
    }
}
