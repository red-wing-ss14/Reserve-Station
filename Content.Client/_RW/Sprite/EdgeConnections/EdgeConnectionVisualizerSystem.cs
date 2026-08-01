using Content.Shared._RW.Sprite.EdgeConnections;
using Robust.Client.GameObjects;

namespace Content.Client._RW.Sprite.EdgeConnections;

public sealed class EdgeConnectionVisualizerSystem : VisualizerSystem<EdgeConnectionComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EdgeConnectionComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData<EdgeConnectionDirections>(uid, EdgeConnectionVisuals.ConnectionMask, out _, args.Component);
    }
}
