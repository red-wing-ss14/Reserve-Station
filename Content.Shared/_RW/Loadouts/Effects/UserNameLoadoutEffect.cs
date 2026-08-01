using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared._RW.Loadouts.Effects;

public sealed partial class UserNameLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<string> AllowedUsers = new();

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
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-effect-username-denied"));
            return false;
        }

        var userName = session.Name;

        // Handle localhost@ prefix
        if (userName.StartsWith("localhost@", StringComparison.OrdinalIgnoreCase))
            userName = userName.Substring("localhost@".Length);

        if (AllowedUsers.Any(allowedUser => string.Equals(userName, allowedUser, StringComparison.OrdinalIgnoreCase)))
            return true;

        reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-effect-username-denied"));
        return false;
    }
}
