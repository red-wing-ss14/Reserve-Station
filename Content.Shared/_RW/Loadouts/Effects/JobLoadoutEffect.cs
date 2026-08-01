using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RW.Loadouts.Effects;

public sealed partial class JobLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<ProtoId<JobPrototype>> Jobs = new();

    [DataField]
    public bool Inverted = false;

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var currentJob = loadout.Role;

        var isInList = Jobs.Any(j => j.Id == currentJob.Id);

        if (isInList ^ Inverted)
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromUnformatted(Loc.GetString("loadout-effect-job-restriction"));
        return false;
    }
}
