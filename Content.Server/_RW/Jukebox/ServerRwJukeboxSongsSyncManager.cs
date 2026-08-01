using System.Collections.Generic;
using System.Text;
using Content.Shared._RW.Jukebox;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server._RW.Jukebox;

public sealed class ServerRwJukeboxSongsSyncManager : RwJukeboxSongsSyncManager
{
    [Dependency] private readonly INetManager _netManager = default!;

    private const int MaxSongNameLength = 64;
    private const string DefaultSongName = "song";

    public override void Initialize()
    {
        base.Initialize();
        _netManager.Connected += OnClientConnected;
    }

    private void OnClientConnected(object? sender, NetChannelArgs e)
    {
        foreach (var (path, data) in ContentRoot.GetAllFiles())
        {
            var msg = new RwJukeboxSongUploadNetMessage
            {
                RelativePath = path,
                Data = data
            };

            e.Channel.SendMessage(msg);
        }
    }

    public (string SongName, ResPath Path) SyncSongData(string songName, List<byte> bytes)
    {
        var safeName = SanitizeSongName(songName);

        while (ContentRoot.TryGetFile(new ResPath(safeName + ".ogg"), out _))
            safeName += "a";

        var msg = new RwJukeboxSongUploadNetMessage
        {
            Data = bytes.ToArray(),
            RelativePath = new ResPath(safeName + ".ogg")
        };

        OnSongUploaded(msg);
        var path = new ResPath($"{Prefix}/{safeName}.ogg");
        return (safeName, path);
    }

    private static string SanitizeSongName(string songName)
    {
        if (string.IsNullOrEmpty(songName))
            return DefaultSongName;

        songName = songName.Replace("..", string.Empty);

        var sb = new StringBuilder(songName.Length);
        foreach (var c in songName)
        {
            if (sb.Length >= MaxSongNameLength)
                break;

            if (IsAllowed(c))
                sb.Append(c);
        }

        var result = sb.ToString().Trim();

        if (string.IsNullOrWhiteSpace(result))
            return DefaultSongName;

        return result;
    }

    private static bool IsAllowed(char c)
    {
        if (c is >= 'a' and <= 'z')
            return true;
        if (c is >= 'A' and <= 'Z')
            return true;
        if (c is >= '0' and <= '9')
            return true;
        if (c == ' ' || c == '_' || c == '-')
            return true;

        if (c is >= 'а' and <= 'я')
            return true;
        if (c is >= 'А' and <= 'Я')
            return true;
        if (c == 'ё' || c == 'Ё')
            return true;

        return false;
    }

    public override void OnSongUploaded(RwJukeboxSongUploadNetMessage message)
    {
        ContentRoot.AddOrUpdateFile(message.RelativePath, message.Data);

        foreach (var channel in _netManager.Channels)
        {
            channel.SendMessage(message);
        }
    }

    public void CleanUp()
    {
        ContentRoot.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _netManager.Connected -= OnClientConnected;

        base.Dispose(disposing);
    }
}
