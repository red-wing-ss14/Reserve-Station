// SPDX-License-Identifier: MIT

using Content.Server._RW.Gulag;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class OOCCommand : LocalizedCommands
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!; // RW

        public override string Command => "ooc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            // RW start
            if (player.AttachedEntity is { Valid: true } entity)
            {
                var regulatingCollar = _entityManager.EntitySysManager.GetEntitySystem<GulagRegulatingCollarSystem>();
                if (regulatingCollar.TryPunishChatMessage(entity))
                    return;
            }

            var gulag = _entityManager.EntitySysManager.GetEntitySystem<GulagSystem>();
            if (gulag.IsUserGulagged(player.UserId))
                return;
            // RW end

            _chatManager.TrySendOOCMessage(player, message, OOCChatType.OOC);
        }
    }
}
