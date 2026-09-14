using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;
using Entropy.Game.Systems;
using Xunit;

namespace Entropy.Game.Tests;

public class GameSaveTests
{
    [Fact]
    public void CapturePreservesWalletBalance()
    {
        var world = new World();
        var player = world.Create();
        world.Set(player, new Position { Value = Vector2.Zero });
        world.Set(player, new Health { Current = 10, Max = 10 });
        world.Set(player, new Hunger { Current = 10, Max = 10 });
        world.Set(player, new Thirst { Current = 10, Max = 10 });
        world.Set(player, new Fatigue { Current = 10, Max = 10 });
        world.Set(player, new Wallet { CashCents = 4321 });

        var save = GameSave.Capture(
            world,
            player,
            seed: 1,
            elapsedMinutes: 0,
            mapId: "test");

        Assert.Equal(4321, save.CashCents);
    }
}
