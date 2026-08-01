using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Content.Server.Database;
using Content.Shared.Database;
using Robust.Server.Player;

namespace Content.IntegrationTests.Tests._RW.Gulag;

[TestFixture]
public sealed class GulagDatabaseTest
{
    [Test]
    public async Task SentenceExpirationUpdateDoesNotOverwriteNewerEdit()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var db = server.ResolveDependency<IServerDbManager>();
        var playerManager = server.ResolveDependency<IPlayerManager>();
        var session = playerManager.Sessions.Single();
        var originalExpiration = DateTimeOffset.UtcNow.AddHours(1);
        var updatedExpiration = originalExpiration.AddHours(1);

        await db.AddBanAsync(new BanDef(
            id: null,
            type: BanType.Server,
            userIds: ImmutableArray.Create(session.UserId),
            addresses: ImmutableArray<(IPAddress address, int cidrMask)>.Empty,
            hwIds: ImmutableArray<ImmutableTypedHwid>.Empty,
            banTime: DateTimeOffset.UtcNow,
            expirationTime: originalExpiration,
            roundIds: ImmutableArray<int>.Empty,
            playtimeAtNote: TimeSpan.Zero,
            reason: "original reason",
            severity: NoteSeverity.Medium,
            banningAdmin: null,
            unban: null));

        var ban = await db.GetBanAsync(null, session.UserId, null, null);
        Assert.That(ban, Is.Not.Null);
        Assert.That(ban.Id, Is.Not.Null);
        var banId = ban.Id.Value;

        Assert.That(await db.TryEditServerBanExpiration(
            banId,
            originalExpiration,
            updatedExpiration,
            session.UserId.UserId,
            DateTimeOffset.UtcNow), Is.True);

        Assert.That(await db.TryEditServerBanExpiration(
            banId,
            originalExpiration,
            updatedExpiration.AddHours(1),
            session.UserId.UserId,
            DateTimeOffset.UtcNow), Is.False);

        var updatedBan = await db.GetBanAsync(banId);
        Assert.That(updatedBan, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updatedBan.ExpirationTime, Is.EqualTo(updatedExpiration));
            Assert.That(updatedBan.Reason, Is.EqualTo("original reason"));
            Assert.That(updatedBan.Severity, Is.EqualTo(NoteSeverity.Medium));
        });

        await db.AddUnbanAsync(new UnbanDef(banId, null, DateTimeOffset.UtcNow));
        await pair.CleanReturnAsync();
    }
}
