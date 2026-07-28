// SPDX-License-Identifier: MIT

namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class DiskConsolePrintingComponent : Component
{
    public TimeSpan FinishTime;

    // RW-Start
    public EntityUid? Actor;

    public EntityUid? Server;

    public int Price;
    // RW-End
}
