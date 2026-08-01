using Content.Client.Message;
using Content.Shared._RW.Discord;
using Robust.Client.UserInterface;using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._RW.Discord;

public sealed class DiscordLinkUIController : UIController
{    [Dependency] private readonly IClipboardManager _clipboard = default!;
    [Dependency] private readonly DiscordLinkManager _linkManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private DiscordLinkWindow? _window;
    private TimeSpan _disableUntil;
    private Guid _code;

    public override void Initialize()
    {
        _linkManager.CodeReceived += OnCodeReceived;
        _linkManager.StatusUpdated += OnStatusUpdated;
    }

    private void OnCodeReceived(Guid code)
    {
        _code = code;

        if (_window == null)
            return;

        _window.CopyButton.Disabled = false;
        UpdateWindowContent();
    }

    private void OnStatusUpdated()
    {
        if (_window == null)
            return;

        UpdateWindowContent();
    }

    public void ToggleWindow()
    {
        if (_window == null)
        {
            _window = new DiscordLinkWindow();
            _window.OnClose += () => _window = null;

            _window.CopyButton.OnPressed += OnCopyPressed;

            UpdateWindowContent();
            _window.OpenCentered();

            if (_code == default)
            {
                _window.CopyButton.Disabled = true;
                _net.ClientSendMessage(new DiscordLinkRequestMsg());
            }

            return;
        }

        _window.Close();
        _window = null;
    }

    private void UpdateWindowContent()
    {
        if (_window == null)
            return;

        if (_linkManager.IsLinked)
        {
            _window.StatusLabel.SetMarkupPermissive(
                $"[color=#00ff00][bold]{Loc.GetString("amour-ui-link-discord-already-linked")}[/bold][/color]");
            _window.InstructionLabel.SetMarkupPermissive(
                Loc.GetString("amour-ui-link-discord-already-linked-text"));
            _window.CopyButton.Disabled = true;
        }
        else
        {
            _window.StatusLabel.SetMarkupPermissive(
                $"[color=#ffaa00][bold]{Loc.GetString("amour-ui-link-discord-not-linked-status")}[/bold][/color]");
            _window.InstructionLabel.SetMarkupPermissive(
                Loc.GetString("amour-ui-link-discord-instructions"));
        }
    }

    private void OnCopyPressed(ButtonEventArgs args)
    {
        if (_code == default)
            return;

        _clipboard.SetText(_code.ToString());
        _window!.CopyButton.Text = Loc.GetString("amour-ui-link-discord-copied");
        _window.CopyButton.Disabled = true;
        _disableUntil = _timing.RealTime.Add(TimeSpan.FromSeconds(3));
    }

    public override void FrameUpdate(FrameEventArgs args)    {
        if (_window == null)
            return;

        var time = _timing.RealTime;
        if (_disableUntil != default && time > _disableUntil)
        {
            _disableUntil = default;
            _window.CopyButton.Text = Loc.GetString("amour-ui-link-discord-copy");
            _window.CopyButton.Disabled = false;
        }
    }
}
