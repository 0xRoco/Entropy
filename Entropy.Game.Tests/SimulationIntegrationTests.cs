using Entropy.Engine.Core;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using Entropy.Simulation;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public sealed class SimulationIntegrationTests
{
    [Fact]
    public void SimulationAdapterUsesRuntimeInterfaceInsteadOfGameContext()
    {
        var constructor = typeof(SimulationRuntime).GetConstructors().Single();

        Assert.DoesNotContain(constructor.GetParameters(), parameter => parameter.ParameterType == typeof(GameContext));
        Assert.Contains(constructor.GetParameters(), parameter => parameter.ParameterType == typeof(IGameRuntimeContext));
    }

    [Fact]
    public void IdenticalSeedProducesIdenticalHeadlessTurnTrace()
    {
        var first = CreateWorldSetupSession(77);
        var second = CreateWorldSetupSession(77);

        for (var turn = 0; turn < 3; turn++)
        {
            Assert.True(first.Simulation.Execute(new WaitCommand()).Succeeded);
            Assert.True(second.Simulation.Execute(new WaitCommand()).Succeeded);
            first.Simulation.Advance(1);
            second.Simulation.Advance(1);
        }

        Assert.Equal(WorldTrace(first.Context.World), WorldTrace(second.Context.World));
    }

    [Fact]
    public void NewGameSharesSchedulerWithSimulationState()
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new DefinitionRegistry();
        definitions.LoadItems(contentPath);
        definitions.LoadCreatures(contentPath);
        definitions.LoadTerrains(contentPath);
        definitions.LoadBuildingTemplates(contentPath);
        definitions.LoadWorldObjects(contentPath);
        definitions.LoadLootTables(contentPath);

        var result = WorldSetup.StartNewGame(
            new Rng(1),
            new MessageLog(),
            definitions,
            6,
            new WorldClock(2001, 3, 12, 7, 30));

        Assert.Same(result.Simulation.Scheduler, result.Simulation.Context.Scheduler);
        Assert.True(result.World.IsAlive(result.Player));

        var session = GameSession.Create(result, new MessageLog(), definitions, new Rng(1), 6);
        Assert.Same(result.Simulation.Scheduler, session.Context.Scheduler);
    }

    [Fact]
    public void NewGameConnectsAuthoredCommonsToSeededOutside()
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new DefinitionRegistry();
        definitions.LoadItems(contentPath);
        definitions.LoadCreatures(contentPath);
        definitions.LoadTerrains(contentPath);
        definitions.LoadBuildingTemplates(contentPath);
        definitions.LoadWorldObjects(contentPath);
        definitions.LoadLootTables(contentPath);

        var result = WorldSetup.StartNewGame(
            new Rng(123),
            new MessageLog(),
            definitions,
            6,
            new WorldClock(2001, 3, 12, 7, 30));

        Assert.Contains("spire_commons", result.Maps.Maps.Keys);
        Assert.Contains("outside_123", result.Maps.Maps.Keys);
        Assert.NotNull(result.Maps.TransitionAt("spire_commons", new Vector2i(0, 15)));
    }

    [Fact]
    public void TemporaryPermitControlsEntryAndIsCapturedBySave()
    {
        var session = CreateWorldSetupSession(321);
        var transition = session.Context.Maps.Transitions.Single(candidate =>
            candidate.FromMap == "checkpoint" && candidate.ToMap == "spire_commons");

        Assert.False(DoorSystem.IsPassable(session.Context, transition));
        Assert.Equal(ActionResult.Turn,
            ArrivalSystem.GrantTemporaryPermit(session.Context, session.Context.Player));
        Assert.True(DoorSystem.IsPassable(session.Context, transition));

        var save = GameSave.Capture(session.Context);

        Assert.NotNull(save.Arrival);
        Assert.True(save.Arrival!.HasTemporaryPermit);
        Assert.Equal("temporary_entrant", save.Arrival.LegalIdentityStatus);

        session.Context.Arrival.HasTemporaryPermit = false;
        session.Context.Arrival.LegalIdentityStatus = "unregistered";
        GameSave.RestorePlayer(session.Context, save);

        Assert.True(session.Context.Arrival.HasTemporaryPermit);
        Assert.Equal("temporary_entrant", session.Context.Arrival.LegalIdentityStatus);
    }

    [Fact]
    public void BriberyGrantsEntryAtTheConfiguredCost()
    {
        var session = CreateWorldSetupSession(654);
        var wallet = session.Context.World.Get<Entropy.Game.Components.Inventory.Wallet>(session.Context.Player);
        var startingCash = wallet.CashCents;

        Assert.Equal(ActionResult.Turn,
            ArrivalSystem.BribeForTemporaryPermit(session.Context, session.Context.Player));
        wallet = session.Context.World.Get<Entropy.Game.Components.Inventory.Wallet>(session.Context.Player);
        Assert.Equal(startingCash - ArrivalSystem.BribeCostCents, wallet.CashCents);
        Assert.True(session.Context.Arrival.HasTemporaryPermit);

        session.Context.Arrival.PermitExpiryMinute = session.Context.Clock.TotalMinutes;
        Assert.False(session.Context.Arrival.HasValidPermit(session.Context.Clock.TotalMinutes));
    }

    [Fact]
    public void AdapterExecutesCommandAndAdvancesAuthoritativeClock()
    {
        var world = new Entropy.Engine.ECS.World();
        var player = world.Create();
        world.Set(player, new Position { Value = new Vector2(1, 1) });
        world.Set(player, new Location { MapId = "test" });

        var map = new TileMap(3, 3);
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, Tile.Floor);
        var maps = new MapGraph();
        maps.AddMap("test", map);
        var clock = new WorldClock(2001, 3, 12, 7, 30);
        var definitions = new DefinitionRegistry();
        var simulation = new SimulationState(new SimulationContext
        {
            World = world,
            Maps = maps,
            Definitions = definitions,
            Rng = new Rng(1),
            Player = player,
            Clock = clock,
            Events = new SimulationEventBus()
        });
        var context = new GameContext
        {
            Map = map,
            MapId = "test",
            Maps = maps,
            Log = new MessageLog(),
            World = world,
            Definitions = definitions,
            Rng = new Rng(1),
            Player = player,
            Clock = clock,
             Scheduler = simulation.Scheduler,
            Visibilities = new Dictionary<string, VisibilityMap> { ["test"] = new VisibilityMap(3, 3) },
            Visibility = new VisibilityMap(3, 3),
            ViewRadius = 6,
            DoorDefinitions = new(),
            DoorStates = new(),
            Simulation = simulation
        };

        var adapter = new SimulationRuntime(simulation, context);

        Assert.True(adapter.Execute(new WaitCommand()).Succeeded);
        Assert.Equal(1, adapter.Advance(1));
        Assert.Equal(1, clock.TotalMinutes);
    }

    [Fact]
    public void RecreatedEntityFallsBackWhenStableIdIsAlreadyOccupied()
    {
        var world = new Entropy.Engine.ECS.World();
        var occupied = world.Create();
        var stableId = world.StableId(occupied);
        var definition = new Entropy.Content.ItemDefinition
        {
            Id = "test_item",
            Name = "Test item",
            Symbol = '?',
            Color = OpenTK.Mathematics.Color4.White
        };

        var recreated = EntitySpawner.CreateItem(world, "test", definition, 0, 0, stableId: stableId);

        Assert.NotEqual(stableId, world.StableId(recreated));
    }

    private static GameSession CreateWorldSetupSession(int seed)
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new DefinitionRegistry();
        definitions.LoadItems(contentPath);
        definitions.LoadCreatures(contentPath);
        definitions.LoadTerrains(contentPath);
        definitions.LoadBuildingTemplates(contentPath);
        definitions.LoadWorldObjects(contentPath);
        definitions.LoadLootTables(contentPath);
        var result = WorldSetup.StartNewGame(
            new Rng(seed),
            new MessageLog(),
            definitions,
            6,
            new WorldClock(2001, 3, 12, 7, 30));
        return GameSession.Create(result, new MessageLog(), definitions, new Rng(seed), 6);
    }

    private static string WorldTrace(Entropy.Engine.ECS.World world) =>
        string.Join(";", world.Query<Location, Position>()
            .OrderBy(world.StableId)
            .Select(entity => $"{world.StableId(entity)}:{world.Get<Location>(entity).MapId}:{world.Get<Position>(entity).Value.X}:{world.Get<Position>(entity).Value.Y}"));
}
