using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._RW.Jukebox;

public sealed class ServerRwJukeboxSongsSyncSystem : EntitySystem
{
    [Dependency] private readonly ServerRwJukeboxSongsSyncManager _jukeboxManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _jukeboxManager.CleanUp());
    }
}
