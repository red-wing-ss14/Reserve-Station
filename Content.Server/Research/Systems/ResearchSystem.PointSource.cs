// SPDX-License-Identifier: MIT

using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared._RW.Research;
using Content.Shared._RW.Research.Components;
using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeSource()
    {
//        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond); // RW-Edit
        SubscribeLocalEvent<ResearchPointSourceComponent, ResearchServerGetPointsPerSecondByTypeEvent>(OnGetPointsPerSecondByType); // RW
    }

/* // RW-Edit: Use OnGetPointsPerSecondByType
    private void OnGetPointsPerSecond(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondEvent args)
    {
        // RW-Start
        if (TryComp<ResearchServerControlStatusComponent>(args.Server, out var status) && !status.GenerationEnabled)
            return;
        // RW-End

        if (CanProduce(source))
            args.Points += source.Comp.PointsPerSecond;
    }
*/

    private bool CanProduce(Entity<ResearchPointSourceComponent> source) // RW-Edit: Was public
    {
        return source.Comp.Active && this.IsPowered(source, EntityManager);
    }

    // RW-Start
    private void OnGetPointsPerSecondByType(Entity<ResearchPointSourceComponent> source, ref ResearchServerGetPointsPerSecondByTypeEvent args)
    {
        if (TryComp<ResearchServerControlStatusComponent>(args.Server, out var status) && !status.GenerationEnabled)
            return;

        if (!CanProduce(source))
            return;

        if (source.Comp.RequiredInfrastructure != null &&
            (!TryComp<TechnologyDatabaseComponent>(args.Server, out var db) ||
             !db.UnlockedInfrastructure.Contains(source.Comp.RequiredInfrastructure)))
        {
            return;
        }

        args.Points.Add(new ResearchPointAmount
        {
            Type = source.Comp.PointType,
            Amount = source.Comp.PointsPerSecond,
        });
    }
    // RW-End
}
