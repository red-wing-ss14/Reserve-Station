using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared._RW.Jukebox;

public sealed class RwJukeboxSharedSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RwJukeboxComponent, ComponentStartup>(OnJukeboxInit);
    }

    private void OnJukeboxInit(EntityUid uid, RwJukeboxComponent component, ComponentStartup args)
    {
        component.TapeContainer =
            _containerSystem.EnsureContainer<Container>(uid, RwJukeboxComponent.JukeboxContainerName);
    }
}
