using Content.Client._RW.ChatTrigger.UI;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._RW.ChatTrigger;
using Content.Shared.Chat;
using Content.Client.Chat.Managers;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RW.ChatTrigger;

public sealed class ChatTriggerSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly Content.Client._RW.Brainrot.RwBrainrotClientSystem _rwBrainrot = default!; // RW

    private ChatUIController? _chatController;

    private GrammarnaziBotWindow? _grammarWindow;
    private BrainrotBotWindow? _brainrotWindow;

    private static readonly ChatSelectChannel IcChannels =
        ChatSelectChannel.Local |
        ChatSelectChannel.Whisper |
        ChatSelectChannel.Radio |
        ChatSelectChannel.Emotes;

    public override void Initialize()
    {
        base.Initialize();

        _chatController = _uiManager.GetUIController<ChatUIController>();
        _chatController.BeforeMessageSent = OnBeforeMessageSent;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_chatController != null)
            _chatController.BeforeMessageSent = null;

        _grammarWindow?.Close();
        _grammarWindow = null;

        _brainrotWindow?.Close();
        _brainrotWindow = null;
    }

    private bool OnBeforeMessageSent(string text, ChatSelectChannel channel)
    {
        if ((channel & IcChannels) != channel)
            return true;

        foreach (var proto in _protoManager.EnumeratePrototypes<GrammarnaziBotTriggerPrototype>())
        {
            if (FindMatchedVariant(text, proto.Trigger) == null)
                continue;

            ShowGrammarWarning(text, Loc.GetString(proto.Description));
            return false;
        }

        foreach (var proto in _protoManager.EnumeratePrototypes<BrainrotBotTriggerPrototype>())
        {
            var matched = FindMatchedVariant(text, proto.Trigger);
            if (matched == null)
                continue;

            ShowBrainrotWarning(text, matched, Loc.GetString(proto.Description));
            return false;
        }

        // RW start
        foreach (var customTrigger in _rwBrainrot.CustomTriggers)
        {
            var matched = FindMatchedVariant(text, customTrigger);
            if (matched == null)
                continue;

            ShowBrainrotWarning(text, matched, Loc.GetString("brainrot-trigger-desc-generic"));
            return false;
        }
        // RW end

        return true;
    }

    private static string? FindMatchedVariant(string text, string trigger)
    {
        if (trigger.StartsWith('[') && trigger.EndsWith(']'))
        {
            var inner = trigger[1..^1];
            foreach (var variant in inner.Split(';'))
            {
                var trimmed = variant.Trim();
                if (MatchesSingleTrigger(text, trimmed))
                    return trimmed;
            }
            return null;
        }

        return MatchesSingleTrigger(text, trigger) ? trigger : null;
    }

    private static bool MatchesSingleTrigger(string text, string trigger)
    {
        if (trigger.Contains(' '))
            return text.Contains(trigger, StringComparison.OrdinalIgnoreCase);

        var idx = 0;
        while (true)
        {
            idx = text.IndexOf(trigger, idx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            var before = idx == 0 || !char.IsLetterOrDigit(text[idx - 1]);
            var after = idx + trigger.Length >= text.Length || !char.IsLetterOrDigit(text[idx + trigger.Length]);

            if (before && after)
                return true;

            idx += trigger.Length;
        }
    }

    private void ShowGrammarWarning(string originalText, string description)
    {
        if (_grammarWindow == null || _grammarWindow.Disposed)
            _grammarWindow = new GrammarnaziBotWindow();

        _grammarWindow.SetDescription(description);
        _grammarWindow.OnSendAnyway = () => ForceSend(originalText);

        if (!_grammarWindow.IsOpen)
            _grammarWindow.OpenCentered();
        else
            _grammarWindow.MoveToFront();
    }

    private void ShowBrainrotWarning(string originalText, string triggerWord, string description)
    {
        if (_brainrotWindow == null || _brainrotWindow.Disposed)
            _brainrotWindow = new BrainrotBotWindow();

        _brainrotWindow.SetTriggerWord(triggerWord);
        _brainrotWindow.SetDescription(description);
        _brainrotWindow.OnSendAnyway = () => ForceSend(originalText);

        if (!_brainrotWindow.IsOpen)
            _brainrotWindow.OpenCentered();
        else
            _brainrotWindow.MoveToFront();
    }

    private void ForceSend(string text)
    {
        if (_chatController == null)
            return;

        var (prefixChannel, splitText, _, _) = _chatController.SplitInputContents(text);
        var channel = prefixChannel != ChatSelectChannel.None
            ? prefixChannel
            : _chatController.GetPreferredChannel();

        var finalText = splitText;
        if (prefixChannel == ChatSelectChannel.None && channel == ChatSelectChannel.Radio)
            finalText = $";{splitText}";

        _chatManager.SendMessage(finalText, channel);
    }
}
