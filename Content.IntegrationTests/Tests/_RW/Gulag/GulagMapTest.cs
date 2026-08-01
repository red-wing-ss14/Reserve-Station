using Content.Server._RW.Gulag.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._RW.Gulag;

[TestFixture]
public sealed class GulagMapTest
{
    private static readonly ResPath GulagMap = new("/Maps/_RW/Gulag/gulag.yml");

    [Test]
    public async Task GulagMapLoads()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapLoader = entManager.System<MapLoaderSystem>();
        var mapSystem = entManager.System<SharedMapSystem>();

        Entity<MapGridComponent>? grid = null;

        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out var mapId, runMapInit: false);
            Assert.That(mapLoader.TryLoadGrid(mapId, GulagMap, out grid), Is.True);
            Assert.That(grid, Is.Not.Null);

            var processorQuery = entManager.AllEntityQueryEnumerator<GulagOreProcessorComponent>();
            Assert.That(processorQuery.MoveNext(out _, out _), Is.True);
        });

        await pair.CleanReturnAsync();
    }
}
