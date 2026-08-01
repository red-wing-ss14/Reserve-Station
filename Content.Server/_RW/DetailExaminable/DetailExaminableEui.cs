using Content.Server.EUI;
using Content.Shared._RW.DetailExaminable;
using Content.Shared.Eui;

namespace Content.Server._RW.DetailExaminable;

//
// License-Identifier: GPL-3.0-or-later
//

public sealed class DetailExaminableEui : BaseEui
{
    private readonly DetailExaminableEuiState _state;

    public DetailExaminableEui(DetailExaminableEuiState state)
    {
        _state = state;
    }

    public override EuiStateBase GetNewState()
    {
        return _state;
    }
}
