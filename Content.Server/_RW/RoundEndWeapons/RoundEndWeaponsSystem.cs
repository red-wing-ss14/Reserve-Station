using Content.Server.Chat.Managers;
using Content.Server.Hands.Systems;
using Content.Shared._RW;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Maths;

namespace Content.Server._RW.RoundEndWeapons;

public sealed class RoundEndWeaponsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public static readonly ProtoId<WeightedRandomEntityPrototype> EndOfRoundWeapons = "EndOfRoundWeapons";

    public static readonly SoundSpecifier EndOfRoundSound =
        new SoundPathSpecifier("/Audio/_RW/Misc/RoundEnd/rezniya.ogg");

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
        Subs.CVar(_cfg, RwCVars.RoundEndWeapons, x => _enabled = x, true);
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        if (!_enabled)
            return;

        var proto = _proto.Index(EndOfRoundWeapons);
        if (proto.Weights.Count == 0)
            return;

        var query =
            EntityQueryEnumerator<HumanoidAppearanceComponent, ActorComponent, HandsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var hands, out var xform))
        {
            RemCompDeferred<PacifiedComponent>(uid);
            var weapon = Spawn(proto.Pick(_random), xform.Coordinates);
            _hands.PickupOrDrop(uid, weapon, handsComp: hands);
        }

        _chatMan.DispatchServerAnnouncement("!!!РЕЗНЯ!!!", Color.Red);
        _audio.PlayGlobal(EndOfRoundSound, Filter.Broadcast(), false);
    }
}
