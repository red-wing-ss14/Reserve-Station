using System.Threading.Tasks;
using Content.Shared._RW.Loadouts;
using Content.Shared._RW.Loadouts.Effects;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._RW.Loadouts;

/// <summary>
/// Client-side Boosty tier manager that receives tier info from the server.
/// </summary>
public sealed class BoostyTierManager : IBoostyTierManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    private BoostyPlayerTier? _cachedTier;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgBoostyTierInfo>(OnBoostyTierInfo);
    }

    private void OnBoostyTierInfo(MsgBoostyTierInfo msg)
    {
        if (msg.IsActive)
        {
            _cachedTier = new BoostyPlayerTier
            {
                IsActive = msg.IsActive,
                TierLevel = msg.TierLevel,
                TierName = msg.TierName
            };
        }
        else
        {
            _cachedTier = null;
        }
    }

    public BoostyPlayerTier? GetPlayerTier(ICommonSession session)
    {
        // On client, we only have info about the local player
        return _cachedTier;
    }

    public Task PreloadPlayerTierAsync(ICommonSession session)
    {
        // Not used on client - server sends the data via network message
        return Task.CompletedTask;
    }

    public void Reset()
    {
        _cachedTier = null;
    }
}
