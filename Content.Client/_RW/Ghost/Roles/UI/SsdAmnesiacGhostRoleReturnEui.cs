using Content.Client.Eui;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._RW.Ghost.Roles.UI;

[UsedImplicitly]
public sealed class SsdAmnesiacGhostRoleReturnEui : BaseEui
{
    private readonly SsdAmnesiacGhostRoleReturnWindow _window;

    public SsdAmnesiacGhostRoleReturnEui()
    {
        _window = new SsdAmnesiacGhostRoleReturnWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _window.Close();
    }
}
