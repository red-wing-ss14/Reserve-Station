using Content.Shared._RW.ReadyManifest;

namespace Content.Client._RW.ReadyManifest;

public sealed class ReadyManifestSystem : EntitySystem
{
    public void RequestReadyManifest()
    {
        RaiseNetworkEvent(new RequestReadyManifestMessage());
    }
}
