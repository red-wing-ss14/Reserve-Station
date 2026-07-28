// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class CargoPalletConsoleInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// estimated apraised value of all the entities on top of pallets on the same grid as the console
    /// </summary>
    public int Appraisal;

    /// <summary>
    /// number of entities on top of pallets on the same grid as the console
    /// </summary>
    public int Count;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    // RW-Start
    public List<CargoPalletMarketChangeData> ActiveChanges;
    public List<CargoPalletMarketChangeData> RecentChanges;
    // RW-End

    public CargoPalletConsoleInterfaceState(
        int appraisal,
        int count,
        bool enabled,
        List<CargoPalletMarketChangeData>? activeChanges = null, // RW
        List<CargoPalletMarketChangeData>? recentChanges = null) // RW
    {
        Appraisal = appraisal;
        Count = count;
        Enabled = enabled;
        ActiveChanges = activeChanges ?? new(); // RW
        RecentChanges = recentChanges ?? new(); // RW
    }
}

// RW-Start
[NetSerializable, Serializable]
public sealed class CargoPalletMarketChangeData
{
    public string MaterialProto;
    public float Multiplier;
    public int Sequence;

    public CargoPalletMarketChangeData(string materialProto, float multiplier, int sequence)
    {
        MaterialProto = materialProto;
        Multiplier = multiplier;
        Sequence = sequence;
    }
}
// RW-End
