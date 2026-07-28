// SPDX-License-Identifier: MIT

using Content.Shared._RW.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;

namespace Content.Client.Research;

public sealed class ResearchSystem : SharedResearchSystem
{
    // RW-Start
    public List<ResearchPointAmount> GetTechnologyFinalPointCostsForUi(TechnologyDatabaseComponent database, TechnologyPrototype technology)
    {
        return GetTechnologyFinalPointCosts(database, technology);
    }
    // RW-End
}
