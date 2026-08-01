// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Preferences;
using Robust.Shared.GameStates;

namespace Content.Shared.DetailExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DetailExaminableComponent : Component
{
    [DataField, AutoNetworkedField] // RW-Edit: Removed: "required: true"
    public string Content = string.Empty;

    // RW-Start
    [DataField, AutoNetworkedField]
    public string CharacterContent { get; set; } = string.Empty;



    [DataField, AutoNetworkedField]
    public string GreenContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string YellowContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string RedContent { get; set; } = string.Empty;
    public void SetProfile(HumanoidCharacterProfile profile)
    {
        Content = profile.FlavorText;
        CharacterContent = profile.CharacterFlavorText;
        GreenContent = profile.GreenFlavorText;
        YellowContent = profile.YellowFlavorText;
        RedContent = profile.RedFlavorText;
    }
    // RW-End
}
