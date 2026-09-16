using Entropy.Content;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.Core;
using Entropy.Engine.World;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class PurchaseSystemTests
{
    [Fact]
    public void PurchaseDeductsPriceAndTransfersItem()
    {
        var fixture = CreateFixture(5000);
        var item = fixture.CreateItem("crackers", 2);

        Assert.True(PurchaseSystem.TryPurchase(fixture.Context, fixture.Player, item, fixture.Shop));
        Assert.Equal(4602, fixture.World.Get<Wallet>(fixture.Player).CashCents);
        Assert.Contains(item, fixture.World.Get<Container>(fixture.Player).Items);
        Assert.Empty(fixture.World.Get<Container>(fixture.Shop).Items);
    }

    [Fact]
    public void InsufficientCashLeavesStockUntouched()
    {
        var fixture = CreateFixture(100);
        var item = fixture.CreateItem("crackers");

        Assert.False(PurchaseSystem.TryPurchase(fixture.Context, fixture.Player, item, fixture.Shop));
        Assert.Equal(100, fixture.World.Get<Wallet>(fixture.Player).CashCents);
        Assert.Contains(item, fixture.World.Get<Container>(fixture.Shop).Items);
    }

    [Fact]
    public void StealingTransfersItemWithoutChargingWallet()
    {
        var fixture = CreateFixture(5000);
        var item = fixture.CreateItem("water_bottle");

        Assert.True(PurchaseSystem.TrySteal(fixture.Context, fixture.Player, item, fixture.Shop));
        Assert.Equal(5000, fixture.World.Get<Wallet>(fixture.Player).CashCents);
        Assert.Contains(item, fixture.World.Get<Container>(fixture.Player).Items);
    }

    [Fact]
    public void NonShopContainersCannotBePurchasedFrom()
    {
        var fixture = CreateFixture(5000, shop: false);
        var item = fixture.CreateItem("crackers");

        Assert.False(PurchaseSystem.TryPurchase(fixture.Context, fixture.Player, item, fixture.Shop));
        Assert.Equal(5000, fixture.World.Get<Wallet>(fixture.Player).CashCents);
    }

    private static Fixture CreateFixture(int cash, bool shop = true)
    {
        var map = new TileMap(5, 5);
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, Tile.Floor);

        var maps = new MapGraph();
        maps.AddMap("test", map);
        var world = new World();
        var player = world.Create();
        world.Set(player, new Position { Value = new Vector2(1, 1) });
        world.Set(player, new Location { MapId = "test" });
        world.Set(player, Container.WithSlots(6));
        world.Set(player, new Wallet { CashCents = cash });

        var container = world.Create();
        world.Set(container, new Position { Value = new Vector2(2, 1) });
        world.Set(container, new Location { MapId = "test" });
        world.Set(container, Container.WithSlots(6));
        world.Set(container, new WorldObjectIdentity { DefinitionId = shop ? "store_shelf" : "shelf", Name = "Shelf" });

        var definitions = new DefinitionRegistry();
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        definitions.LoadItems(contentPath);
        definitions.LoadWorldObjects(contentPath);

        var visibility = new VisibilityMap(5, 5);
        return new Fixture(world, player, container, new GameContext
        {
            Map = map,
            MapId = "test",
            Maps = maps,
            Log = new MessageLog(),
            World = world,
            Definitions = definitions,
            Rng = new Entropy.Engine.Core.Rng(1234),
            Player = player,
            Clock = new WorldClock(2001, 3, 12, 7, 30),
             Scheduler = new ActorScheduler(),
            Visibilities = new Dictionary<string, VisibilityMap> { ["test"] = visibility },
            Visibility = visibility,
            ViewRadius = 6,
            DoorDefinitions = new(),
            DoorStates = new()
        });
    }

    private sealed class Fixture(World world, Entity player, Entity shop, GameContext context)
    {
        public World World { get; } = world;
        public Entity Player { get; } = player;
        public Entity Shop { get; } = shop;
        public GameContext Context { get; } = context;

        public Entity CreateItem(string id, int count = 1)
        {
            var item = World.Create();
            World.Set(item, new Item());
            World.Set(item, new ItemIdentity { DefinitionId = id, Name = id });
            if (count > 1)
                World.Set(item, new Stackable { Count = count, MaxStack = 10 });
            Assert.True(ItemSystem.Transfer(World, item, Shop));
            return item;
        }
    }
}
