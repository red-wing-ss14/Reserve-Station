using Content.Shared._RW.TTS;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared._RW.Surgery;

[RegisterComponent]
public sealed partial class ChangedSexComponent : Component
{
    [ViewVariables] public Sex OldSex { get; set; } = Sex.Male;
    [ViewVariables] public Gender OldGender { get; set; } = Gender.Male;
    [ViewVariables] public ProtoId<TTSVoicePrototype> OldVoice { get; set; } = string.Empty;
}
