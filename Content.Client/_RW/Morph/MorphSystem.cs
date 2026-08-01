using Content.Shared._RW.Morph;
using Content.Shared.Alert.Components;

namespace Content.Client._RW.Morph;

public sealed class MorphSystem : SharedMorphSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorphComponent, GetGenericAlertCounterAmountEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<MorphComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.BiomassAlert != args.Alert)
            return;

        args.Amount = ent.Comp.Biomass.Int();
    }
}
