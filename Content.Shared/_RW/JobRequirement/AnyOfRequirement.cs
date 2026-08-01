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
public sealed partial class AnyOfRequirement : JobRequirement
{
    [DataField(required: true)]
    public List<JobRequirement> Requirements = new();

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var timeRequirementReasons = new List<FormattedMessage>();
        var hasDiscordRequirement = false;
        var discordCheckPassed = false;
        
        foreach (var requirement in Requirements)
        {
            var isDiscord = requirement is Content.Shared._RW.Discord.DiscordLinkRequirement;
            
            if (isDiscord)
                hasDiscordRequirement = true;
            
            if (requirement.Check(entManager, protoManager, profile, playTimes, out var subReason))
            {
                if (isDiscord)
                    discordCheckPassed = true;
                    
                reason = null;
                return !Inverted;
            }

            if (subReason != null && !isDiscord)
            {
                timeRequirementReasons.Add(subReason);
            }
        }

        if (!Inverted)
        {
            reason = new FormattedMessage();

            if (hasDiscordRequirement && timeRequirementReasons.Count > 0)
            {
                reason.AddText(Loc.GetString("role-timer-discord-link-required"));
                reason.PushNewline();
                reason.AddText(Loc.GetString("role-timer-or-alternative"));
                reason.PushNewline();
                
                for (var i = 0; i < timeRequirementReasons.Count; i++)
                {
                    if (i > 0)
                        reason.PushNewline();
                    reason.AddMessage(timeRequirementReasons[i]);
                }
            }
            else if (timeRequirementReasons.Count > 0)
            {
                for (var i = 0; i < timeRequirementReasons.Count; i++)
                {
                    if (i > 0)
                        reason.PushNewline();
                    reason.AddMessage(timeRequirementReasons[i]);
                }
            }
            else if (hasDiscordRequirement)
            {
                reason.AddText(Loc.GetString("role-timer-discord-link-required"));
            }
            else
            {
                reason.AddText(Loc.GetString("role-timer-any-of-requirement-failed"));
            }

            return false;
        }

        reason = null;
        return true;
    }
}
