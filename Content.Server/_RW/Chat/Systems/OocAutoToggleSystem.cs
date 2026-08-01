using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._RW.CCVar;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._RW.Chat.Systems;

public sealed class OocAutoToggleSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private bool _enabled;
    private int _playerThreshold;

    public override void Initialize()
    {
        base.Initialize();

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        Subs.CVar(_cfg, RwCCVars.OocAutoTogglePlayerThreshold, OnPlayerThresholdChanged, true);
        Subs.CVar(_cfg, RwCCVars.OocAutoToggleEnabled, OnEnabledChanged, true);
        Subs.CVar(_cfg, CCVars.OocEnabled, OnOocEnabledChanged);

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerThresholdChanged(int value)
    {
        _playerThreshold = Math.Max(0, value);
        RefreshInRoundOocState();
    }

    private void OnEnabledChanged(bool value)
    {
        _enabled = value;
        RefreshOocStateForRunLevel();
    }

    private void OnOocEnabledChanged(bool value)
    {
        if (!_enabled ||
            _gameTicker.RunLevel != GameRunLevel.InRound ||
            value == ShouldEnableOocInRound())
            return;

        RefreshInRoundOocState();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        RefreshInRoundOocState();
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent args)
    {
        RefreshOocStateForRunLevel();
    }

    private bool ShouldEnableOocInRound()
    {
        return _player.PlayerCount <= _playerThreshold;
    }

    private void RefreshOocStateForRunLevel()
    {
        if (!_enabled)
            return;

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            SetOocEnabled(true);
            return;
        }

        RefreshInRoundOocState();
    }

    private void RefreshInRoundOocState()
    {
        if (!_enabled || _gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        SetOocEnabled(ShouldEnableOocInRound());
    }

    private void SetOocEnabled(bool desired)
    {
        if (_cfg.GetCVar(CCVars.OocEnabled) == desired)
            return;

        _chat.SetOocEnabledSilently(desired);
    }
}
