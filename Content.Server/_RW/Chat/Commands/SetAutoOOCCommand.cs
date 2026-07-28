using Content.Server.Administration;
using Content.Shared._RW.CCVar;
using Content.Shared.Administration;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server._RW.Chat.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SetAutoOOCCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => "setautoooc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 0), ("upper", 1)));
            return;
        }

        var enabled = _cfg.GetCVar(RwCCVars.OocAutoToggleEnabled);

        if (args.Length == 0)
            enabled = !enabled;

        if (args.Length == 1 && !bool.TryParse(args[0], out enabled))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        _cfg.SetCVar(RwCCVars.OocAutoToggleEnabled, enabled);

        shell.WriteLine(Loc.GetString(enabled ? "cmd-setautoooc-enabled" : "cmd-setautoooc-disabled"));
    }
}
