using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class AllOfRequirement : JobRequirement
{
    [DataField(required: true)]
    public List<JobRequirement> Requirements = new();

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var reasons = new List<FormattedMessage>();

        foreach (var requirement in Requirements)
        {
            if (!requirement.Check(entManager, protoManager, profile, playTimes, out var subReason))
            {
                if (subReason != null)
                    reasons.Add(subReason);
            }
        }

        if (reasons.Count > 0 && !Inverted)
        {
            reason = new FormattedMessage();
            for (var i = 0; i < reasons.Count; i++)
            {
                if (i > 0)
                    reason.PushNewline();
                reason.AddMessage(reasons[i]);
            }
            return false;
        }

        if (reasons.Count == 0 && Inverted)
        {
            reason = new FormattedMessage();
            reason.AddText(Loc.GetString("role-timer-all-of-requirement-failed"));
            return false;
        }

        reason = null;
        return !Inverted;
    }
}
