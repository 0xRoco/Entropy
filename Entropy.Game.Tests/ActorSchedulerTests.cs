using Entropy.Engine.ECS;
using Entropy.Simulation;
using Xunit;

namespace Entropy.Game.Tests;

public class ActorSchedulerTests
{
    [Fact]
    public void ActorsAreUniqueAndEnergyCanBeSpent()
    {
        var world = new World();
        var actor = world.Create();
        var scheduler = new ActorScheduler();

        scheduler.Add(actor);
        scheduler.Add(actor);
        scheduler.AddEnergy(_ => 100);

        Assert.Single(scheduler.Actors);
        Assert.True(scheduler.Spend(actor, 100));
        Assert.False(scheduler.Spend(actor, 100));
    }

    [Fact]
    public void DeadActorsAreRemovedFromSchedule()
    {
        var world = new World();
        var actor = world.Create();
        var scheduler = new ActorScheduler();
        scheduler.Add(actor);

        world.Destroy(actor);
        scheduler.RemoveDead(world);

        Assert.Empty(scheduler.Actors);
    }

    [Fact]
    public void ReadyLoopDelegatesActionsAfterSpendingEnergy()
    {
        var world = new World();
        var actor = world.Create();
        var scheduler = new ActorScheduler();
        scheduler.Add(actor);
        scheduler.AddEnergy(_ => 100);
        var actions = 0;

        scheduler.RunReady(
            default,
            100,
            candidate => candidate.Equals(actor),
            _ =>
            {
                actions++;
                return ActionResult.Turn;
            },
            () => actions > 0);

        Assert.Equal(1, actions);
    }
}
