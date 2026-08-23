using Robust.Shared.Configuration;

namespace Content.Shared._RW;

[CVarDefs]
public sealed class RwCVars
{
    /*
     * Jukebox
     */

    public static readonly CVarDef<float> JukeboxVolume =
        CVarDef.Create("redwing.jukebox_volume", 0.5f, CVar.ARCHIVE | CVar.CLIENTONLY);

    public static readonly CVarDef<double> MaxJukeboxSongSizeInMb =
        CVarDef.Create("redwing.max_jukebox_song_size_mb", 10.0d, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);

    /*
     * Round end
     */

    /// <summary>
    ///     Should players get a random weapon on roundend
    /// </summary>
    public static readonly CVarDef<bool> RoundEndWeapons =
        CVarDef.Create("maid.round_end_weapons_enabled", true, CVar.SERVERONLY);
}
