using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared._RW.Loadouts.Effects;

/// <summary>
/// Loadout effect that restricts the loadout to Boosty subscribers with a specific tier level.
/// </summary>
public sealed partial class BoostyTierLoadoutEffect : LoadoutEffect
{
    [DataField]
    public int MinTierLevel = 1;

    [DataField]
    public List<string>? AllowedTiers;

    [DataField]
    public string? DeniedReason;

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        // If no session, deny
        if (session == null)
        {
            reason = FormattedMessage.FromMarkupOrThrow(
                Loc.GetString("loadout-effect-boosty-no-session"));
            return false;
        }

        // Try to get the booster tier manager from IoC
        if (!collection.TryResolveType<IBoostyTierManager>(out var tierManager))
        {
            // Tier manager not registered - deny access
            reason = FormattedMessage.FromMarkupOrThrow(
                Loc.GetString("loadout-effect-boosty-no-subscription"));
            return false;
        }

        var tierInfo = tierManager.GetPlayerTier(session);

        // Debug logging
        var sawmill = Logger.GetSawmill("amour.boosty.loadout");
        sawmill.Debug($"Validating loadout for {session.Name}: TierInfo={tierInfo != null}, IsActive={tierInfo?.IsActive}, TierLevel={tierInfo?.TierLevel}, TierName={tierInfo?.TierName}, Required={MinTierLevel}");

        if (tierInfo == null || !tierInfo.IsActive)
        {
            sawmill.Debug($"Denied: tierInfo is null or not active");
            reason = FormattedMessage.FromMarkupOrThrow(
                DeniedReason ?? Loc.GetString("loadout-effect-boosty-no-subscription"));
            return false;
        }

        if (AllowedTiers != null)
        {
            if (AllowedTiers.Count == 0)
            {
                reason = FormattedMessage.FromMarkupOrThrow(
                    DeniedReason ?? Loc.GetString("loadout-effect-boosty-no-subscription"));
                return false;
            }

            var hasAllowedTier = tierInfo.TierName != null && 
                AllowedTiers.Any(t => string.Equals(t, tierInfo.TierName, StringComparison.OrdinalIgnoreCase));
            
            if (!hasAllowedTier)
            {
                reason = FormattedMessage.FromMarkupOrThrow(
                    DeniedReason ?? Loc.GetString("loadout-effect-boosty-tier-required", 
                        ("tiers", string.Join(", ", AllowedTiers))));
                return false;
            }
            
            return true;
        }

        // Check minimum tier level
        if (tierInfo.TierLevel < MinTierLevel)
        {
            reason = FormattedMessage.FromMarkupOrThrow(
                DeniedReason ?? Loc.GetString("loadout-effect-boosty-higher-tier-required",
                    ("required", MinTierLevel),
                    ("current", tierInfo.TierLevel)));
            return false;
        }

        return true;
    }
}

public interface IBoostyTierManager
{
    void Initialize() { }

    void Reset() { }
    BoostyPlayerTier? GetPlayerTier(ICommonSession session);
    Task PreloadPlayerTierAsync(ICommonSession session);
}
public sealed class BoostyPlayerTier
{
    public string? TierName { get; init; }
 
    public int TierLevel { get; init; }

    public bool IsActive { get; init; }
}
