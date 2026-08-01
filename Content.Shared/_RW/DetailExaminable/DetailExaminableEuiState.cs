using Content.Shared.Eui;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RW.DetailExaminable;

//
// License-Identifier: GPL-3.0-or-later
//

[Serializable, NetSerializable]
public sealed class DetailExaminableEuiState : EuiStateBase
{
    public NetEntity Target;
    public string Name;
    public ProtoId<SpeciesPrototype> Species;
    public Sex Sex;
    public Gender Gender;
    public string FlavorText;
    public string CharacterFlavorText;
    public string GreenFlavorText;
    public string YellowFlavorText;
    public string RedFlavorText;

    public DetailExaminableEuiState(
        NetEntity target,
        string name,
        ProtoId<SpeciesPrototype> species,
        Sex sex,
        Gender gender,
        string flavorText,
        string characterFlavorText,
        string greenFlavorText,
        string yellowFlavorText,
        string redFlavorText
    )
    {
        Target = target;
        Name = name;
        Species = species;
        Sex = sex;
        Gender = gender;
        FlavorText = flavorText;
        CharacterFlavorText = characterFlavorText;
        GreenFlavorText = greenFlavorText;
        YellowFlavorText = yellowFlavorText;
        RedFlavorText = redFlavorText;
    }
}
