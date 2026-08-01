using Content.Shared._RW.Jukebox;

namespace Content.Client._RW.Jukebox;

public sealed class ClientRwJukeboxSongsSyncManager : RwJukeboxSongsSyncManager
{
    public override void OnSongUploaded(RwJukeboxSongUploadNetMessage message)
    {
        ContentRoot.AddOrUpdateFile(message.RelativePath, message.Data);
    }
}
