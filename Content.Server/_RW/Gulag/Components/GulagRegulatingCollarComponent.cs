using Robust.Shared.Audio;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._RW.Gulag.Components;

[RegisterComponent, Access(typeof(GulagRegulatingCollarSystem))]
public sealed partial class GulagRegulatingCollarComponent : Component
{
    [DataField]
    public int ShockDamage = 5;

    [DataField]
    public TimeSpan ShockTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan HarmSentenceExtension = TimeSpan.FromMinutes(5);

    [DataField]
    public TimeSpan LiquidationSentenceExtension = TimeSpan.FromHours(24);

    [DataField]
    public LocId HarmWarningMessage = "gulag-regulating-collar-harm-warning";

    [DataField]
    public LocId SpeechPunishmentMessage = "gulag-regulating-collar-speech-punishment";

    [DataField]
    public LocId LiquidationMessage = "gulag-regulating-collar-liquidation";

    [DataField]
    public SoundSpecifier BeepSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    [DataField]
    public int BeepCount = 3;

    [DataField]
    public TimeSpan BeepInterval = TimeSpan.FromSeconds(0.4);

    [DataField]
    public EntProtoId LiquidationEffect = "GulagRegulatingCollarExplosionEffect";

    public bool WarnedForHarm;

    public bool LiquidationTriggered;
}
