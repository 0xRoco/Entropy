using Entropy.Engine.ECS;
using Xunit;

namespace Entropy.Game.Tests;

public class StableReferenceTests
{
    [Fact]
    public void StableIdResolvesAfterRuntimeIdsDiffer()
    {
        var firstWorld = new World();
        firstWorld.Create();
        var first = firstWorld.Create();
        var stableId = firstWorld.StableId(first);

        var secondWorld = new World();
        secondWorld.Create();
        var second = secondWorld.Create(stableId);

        Assert.Equal(stableId, secondWorld.StableId(second));
        Assert.Equal(second, secondWorld.ResolveStableId(stableId));
        Assert.Equal(second, new StableEntityReference(stableId).Resolve(secondWorld));
    }

    [Fact]
    public void DestroyedStableIdsDoNotResolve()
    {
        var world = new World();
        var entity = world.Create();
        var stableId = world.StableId(entity);

        world.Destroy(entity);

        Assert.Equal(default, world.ResolveStableId(stableId));
    }
}
