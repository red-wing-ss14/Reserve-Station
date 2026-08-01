using Robust.Shared.GameStates;

namespace Content.Shared._RW.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HemomancerComponent : VampireClassComponent
{
    [AutoNetworkedField]
    public bool InSanguinePool = false;
    [AutoNetworkedField]
    public bool BloodBringersRiteActive = false;
    public int BloodBringersRiteLoopId = 0;
}