// SPDX-FileCopyrightText: 2025 MirageEexe <Mirageeexe@gmail.com>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;

namespace Content.Goobstation.Shared.MartialArts.Components;

[RegisterComponent]
public sealed partial class GrantArbComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.ArmyHandCombat;

    public override LocId? LearnMessage { get; set; } = "arb-success-learned";
}

