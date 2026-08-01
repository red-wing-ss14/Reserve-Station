using System.Linq;
using Content.Server.Administration;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._RW.Registry.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed class RegSessionCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "regsession";
    public override string Description => "Register a client session entry in the registry.";
    public override string Help => "Usage: regsession <username|guid> [note]";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        var target = args[0];
        var note = args.Length >= 2 ? string.Join(" ", args.Skip(1)) : null;
        var registeredBy = shell.Player?.Name ?? "Console";

        Guid? clientId = null;

        if (Guid.TryParse(target, out var parsed))
        {
            clientId = parsed;
        }
        else
        {
            if (_playerManager.TryGetSessionByUsername(target, out var session))
                clientId = session.UserId.UserId;
        }

        if (clientId == null)
        {
            shell.WriteError($"Could not resolve '{target}'. Provide a valid username (if online) or a GUID.");
            return;
        }

        try
        {
            var alreadyExists = await _db.HasClientRecord(clientId.Value);
            if (alreadyExists)
            {
                shell.WriteError($"A registry entry already exists for {clientId.Value}.");
                return;
            }

            await _db.AddClientRecord(clientId.Value, registeredBy, note);
            shell.WriteLine($"Registry entry added for {clientId.Value}" + (note != null ? $" (note: {note})" : "."));
        }
        catch (Exception)
        {
            shell.WriteError("Database error.");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var names = _playerManager.Sessions.Select(s => s.Name);
            return CompletionResult.FromHintOptions(names, "<username|guid>");
        }

        return CompletionResult.FromHint("[note]");
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class UnregSessionCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "unregsession";
    public override string Description => "Remove a client session entry from the registry.";
    public override string Help => "Usage: unregsession <username|guid>";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        var target = args[0];
        Guid? clientId = null;

        if (Guid.TryParse(target, out var parsed))
        {
            clientId = parsed;
        }
        else
        {
            if (_playerManager.TryGetSessionByUsername(target, out var session))
                clientId = session.UserId.UserId;
        }

        if (clientId == null)
        {
            shell.WriteError($"Could not resolve '{target}'. Provide a valid username (if online) or a GUID.");
            return;
        }

        try
        {
            var removed = await _db.RemoveClientRecord(clientId.Value);
            if (removed)
            {
                shell.WriteLine($"Registry entry removed for {clientId.Value}.");
            }
            else
                shell.WriteError($"No registry entry found for {clientId.Value}.");
        }
        catch (Exception)
        {
            shell.WriteError("Database error.");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var names = _playerManager.Sessions.Select(s => s.Name);
            return CompletionResult.FromHintOptions(names, "<username|guid>");
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class SessionListCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;

    public override string Command => "sessionlist";
    public override string Description => "List all entries in the client session registry.";
    public override string Help => "Usage: sessionlist";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            var records = await _db.GetClientRecords();

            if (records.Count == 0)
            {
                shell.WriteLine("Session registry is empty.");
                return;
            }

            shell.WriteLine($"=== Session Registry ({records.Count} entries) ===");
            foreach (var (clientId, recordedAt, recordedBy, note) in records)
            {
                var noteStr = note != null ? $" | Note: {note}" : "";
                shell.WriteLine($"  {clientId} | Registered: {recordedAt:yyyy-MM-dd HH:mm} UTC | By: {recordedBy}{noteStr}");
            }
        }
        catch (Exception)
        {
            shell.WriteError("Database error.");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}
