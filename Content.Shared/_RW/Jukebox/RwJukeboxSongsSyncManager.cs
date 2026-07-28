using System;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._RW.Jukebox;

public abstract class RwJukeboxSongsSyncManager : IDisposable
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] protected readonly IResourceManager ResourceManager = default!;

    public static readonly ResPath Prefix = ResPath.Root / "RwJukebox";

    protected readonly MemoryContentRoot ContentRoot = new();

    private bool _disposed;

    public virtual void Initialize()
    {
        ResourceManager.AddRoot(Prefix, ContentRoot);

        _netManager.RegisterNetMessage<RwJukeboxSongUploadNetMessage>(OnSongUploaded);
    }

    public abstract void OnSongUploaded(RwJukeboxSongUploadNetMessage message);

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            ContentRoot.Dispose();

        _disposed = true;
    }
}
