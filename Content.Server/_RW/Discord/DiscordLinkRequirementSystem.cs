using Content.Server.GameTicking.Events;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RW.Discord;

public sealed class DiscordLinkRequirementSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IDiscordLinkChecker _discordLinkChecker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("discord_link");
        SubscribeLocalEvent<IsRoleAllowedEvent>(OnIsRoleAllowed);
    }

    private void OnIsRoleAllowed(ref IsRoleAllowedEvent ev)
    {
        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            return;
        if (ev.Requirements != null)
        {
            foreach (var requirement in ev.Requirements)
            {
                if (requirement is not Content.Shared._RW.Discord.DiscordLinkRequirement { Inverted: false })
                    continue;

                if (!_discordLinkChecker.IsDiscordLinkedCached(ev.Player.UserId))
                {
                    ev.Cancelled = true;
                    _sawmill.Warning($"Cancelled role requirement check for {ev.Player.Name} ({ev.Player.UserId}) - Discord account not linked in cache");
                    _ = EntityManager.System<DiscordLinkSystem>().SendLinkStatus(ev.Player);
                }

                return;
            }
        }

        if (ev.Jobs == null)
            return;

        var roleSystem = EntityManager.System<SharedRoleSystem>();

        foreach (var jobId in ev.Jobs)
        {
            if (!_prototypeManager.TryIndex<JobPrototype>(jobId, out var job))
                continue;

            var requirements = roleSystem.GetRoleRequirements(job);
            if (requirements == null)
                continue;

            foreach (var requirement in requirements)
            {
                if (requirement is not Content.Shared._RW.Discord.DiscordLinkRequirement { Inverted: false })
                    continue;

                if (!_discordLinkChecker.IsDiscordLinkedCached(ev.Player.UserId))
                {
                    ev.Cancelled = true;
                    _sawmill.Warning($"Cancelled role '{jobId}' for {ev.Player.Name} ({ev.Player.UserId}) - Discord account not linked in cache");
                    _ = EntityManager.System<DiscordLinkSystem>().SendLinkStatus(ev.Player);
                }

                return;
            }
        }
    }
}
