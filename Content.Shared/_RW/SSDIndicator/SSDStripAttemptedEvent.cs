namespace Content.Shared._RW.SSDIndicator;

public sealed class SSDStripAttemptedEvent(EntityUid user, TimeSpan duration) : EntityEventArgs
{
    public readonly EntityUid User = user;
    public readonly TimeSpan Duration = duration;
}
