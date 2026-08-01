using System;
using System.Threading.Tasks;
using Robust.Shared.IoC;
using JetBrains.Annotations;

namespace Content.Server._RW.Discord;

[UsedImplicitly]
public sealed class DiscordOocBridgeSystem : EntitySystem
{
    [Dependency] private readonly DiscordOocBridgeService _bridge = default!;

    public override void Initialize()
    {
        base.Initialize();

        Task.Run(async () =>
        {
            try
            {
                await _bridge.StartAsync();
            }
            catch (Exception ex)
            {
                Logger.ErrorS("discord-ooc", $"Failed to start Discord OOC bridge: {ex}");
            }
        });
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _bridge.StopAsync().Wait();
    }

    public void OnGameOocMessage(string playerName, string message)
    {
        _ = _bridge.SendToDiscordAsync(playerName, message);
    }
}
