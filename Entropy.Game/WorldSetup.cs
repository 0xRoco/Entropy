using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Game.Components.AI;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using Entropy.Game.WorldGen;
using Entropy.Simulation;
using OpenTK.Mathematics;

namespace Entropy.Game;

public class WorldSetup
{
        public record NewGameResult(
        TileMap Map,
        string MapId,
        MapGraph Maps,
        Dictionary<string, VisibilityMap> Visibilities,
        World World,
            Entity Player,
            IReadOnlyDictionary<string, BuildingInstance> Buildings,
            SimulationState Simulation);

    public static NewGameResult StartNewGame(
        Rng rng,
        MessageLog log,
        DefinitionRegistry defs,
        int viewRadius,
        WorldClock clock)
    {
        var world = new World();
        var scheduler = new ActorScheduler();

        const string mapId = "spire_commons";
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Content", "Maps", "spire_commons.json");
        var authored = AuthoredMapLoader.Load(mapPath, defs);
        var checkpointPath = Path.Combine(AppContext.BaseDirectory, "Content", "Maps", "checkpoint.json");
        var checkpoint = AuthoredMapLoader.Load(checkpointPath, defs);
        var maps = new MapGraph();
        maps.AddMap(mapId, authored.Map);
        maps.AddMap(checkpoint.Definition.Id, checkpoint.Map);

        var outsideMapId = $"outside_{rng.Seed}";
        var outside = ProceduralOutsideGenerator.Generate(
            defs,
            new Rng(unchecked(rng.Seed ^ 0x5EED5EED)),
            96,
            64);
        maps.AddMap(outsideMapId, outside.Map);
        var authoredMaps = new Dictionary<string, AuthoredMap>(StringComparer.OrdinalIgnoreCase)
        {
            [authored.Definition.Id] = authored,
            [checkpoint.Definition.Id] = checkpoint
        };
        foreach (var source in authoredMaps.Values)
        foreach (var transition in source.Definition.Transitions)
        {
            var targetMap = transition.TargetMap.Equals("outside", StringComparison.OrdinalIgnoreCase)
                ? outsideMapId
                : transition.TargetMap;
            var targetTile = transition.TargetAnchor.Equals("west_gate", StringComparison.OrdinalIgnoreCase)
                ? new Vector2i(outside.Map.Width - 1, outside.Map.Height / 2)
                : Anchor(authoredMaps[targetMap].Definition, transition.TargetAnchor);
            maps.Connect(
                source.Definition.Id,
                new Vector2i(transition.X, transition.Y),
                targetMap,
                targetTile);
        }

        var startMapId = outsideMapId;
        var startMap = outside.Map;

        var visibilities = maps.Maps.ToDictionary(
            pair => pair.Key,
            pair => new VisibilityMap(pair.Value.Width, pair.Value.Height));

        var playerStart = new Vector2i(startMap.Width - 6, startMap.Height / 2);
        var player = EntitySpawner.CreatePlayer(
            world,
            startMapId,
            defs.Creature("player"),
            playerStart.X,
            playerStart.Y);

        scheduler.Add(player);

        foreach (var placement in authored.Definition.Objects)
        {
            var objectDef = defs.WorldObject(placement.Definition);
            var spawned = EntitySpawner.CreateWorldObject(
                world,
                mapId,
                objectDef,
                placement.X,
                placement.Y,
                placement.Id);

            foreach (var itemId in objectDef.StarterItems)
                EntitySpawner.SpawnIntoContainer(world, spawned, defs.Item(itemId));

            if (objectDef.LootTableId is { Length: > 0 } lootTableId)
                LootSystem.Generate(world, defs, rng, spawned, defs.LootTable(lootTableId));
        }

        foreach (var placement in checkpoint.Definition.Objects)
        {
            var objectDef = defs.WorldObject(placement.Definition);
            EntitySpawner.CreateWorldObject(
                world,
                checkpoint.Definition.Id,
                objectDef,
                placement.X,
                placement.Y,
                placement.Id);
        }

        var civilian = defs.Creature("human_civilian");

        var guardPost = Anchor(checkpoint.Definition, "guard_post");
        var guard = EntitySpawner.CreateHuman(
            world,
            checkpoint.Definition.Id,
            civilian,
            guardPost.X,
            guardPost.Y,
            "Checkpoint Guard",
            "guard:checkpoint");
        guard.With(world, ShiftWork(6 * 60, 18 * 60, 22 * 60, 5 * 60));
        scheduler.Add(guard);

        var danaHome = Anchor(authored.Definition, "dana_home");
        var danaBed = Anchor(authored.Definition, "dana_bed");
        var storeWork = Anchor(authored.Definition, "store_work");

        var dana = EntitySpawner.CreateHuman(
            world,
            mapId,
            civilian,
            danaBed.X,
            danaBed.Y,
            "Dana");

        dana.With(world, new Home
        {
            MapId = mapId,
            Tile = danaHome
        });

        dana.With(world, new Workplace
        {
            MapId = mapId,
            Tile = storeWork
        });

        dana.With(world, ShiftWork(
            workStart: 8 * 60,
            workEnd: 17 * 60,
            sleepStart: 22 * 60,
            sleepEnd: 7 * 60));

        scheduler.Add(dana);

        var marcusHome = Anchor(authored.Definition, "marcus_home");
        var marcusBed = Anchor(authored.Definition, "marcus_bed");

        var marcus = EntitySpawner.CreateHuman(
            world,
            mapId,
            civilian,
            marcusBed.X,
            marcusBed.Y,
            "Marcus");

        marcus.With(world, new Home
        {
            MapId = mapId,
            Tile = marcusHome
        });

        marcus.With(world, new Workplace
        {
            MapId = mapId,
            Tile = storeWork
        });

        marcus.With(world, ShiftWork(
            workStart: 22 * 60,
            workEnd: 6 * 60,
            sleepStart: 8 * 60,
            sleepEnd: 16 * 60));

        scheduler.Add(marcus);

        var priya = EntitySpawner.CreateHuman(
            world,
            mapId,
            civilian,
            danaHome.X - 1,
            danaHome.Y,
            "Priya");

        priya.With(world, new Home
        {
            MapId = mapId,
            Tile = danaHome
        });

        priya.With(world, SleepOnly(
            sleepStart: 23 * 60,
            sleepEnd: 7 * 60));

        scheduler.Add(priya);

        var keyTile = Anchor(authored.Definition, "quest_key");
        EntitySpawner.CreateItem(
            world,
            mapId,
            defs.Item("key_pharmacy"),
            keyTile.X,
            keyTile.Y);

        var pharmacyShelf = Anchor(authored.Definition, "pharmacy_shelf");
        EntitySpawner.CreateItem(
            world,
            mapId,
            defs.Item("first_aid_kit"),
            pharmacyShelf.X,
            pharmacyShelf.Y);

        var hardwareShelf = Anchor(authored.Definition, "hardware_shelf");
        EntitySpawner.CreateItem(
            world,
            mapId,
            defs.Item("baseball_bat"),
            hardwareShelf.X,
            hardwareShelf.Y);

        var zombie = EntitySpawner.CreateHuman(
            world,
            mapId,
            defs.Creature("zombie"),
            Anchor(authored.Definition, "zombie_spawn").X,
            Anchor(authored.Definition, "zombie_spawn").Y,
            "Zombie");
        scheduler.Add(zombie);

        Fov.Compute(
            playerStart,
            viewRadius,
            startMap,
            visibilities[startMapId]);

        log.Add($"Seed: {rng.Seed}", Color4.LightGray);
        log.Add("The Spire checkpoint waits beyond the road.", Color4.Red);

        var simulation = new SimulationState(new SimulationContext
        {
            World = world,
            Maps = maps,
            Definitions = defs,
            Rng = rng,
            Player = player,
            Clock = clock,
            Events = new SimulationEventBus(),
            Scheduler = scheduler
        });

        return new NewGameResult(
            startMap,
            startMapId,
            maps,
            visibilities,
            world,
            player,
            new Dictionary<string, BuildingInstance>(),
            simulation);
    }

    private static Vector2i Anchor(MapDefinition definition, string id)
    {
        var anchor = definition.Anchors.SingleOrDefault(candidate =>
            candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return anchor is null
            ? throw new InvalidOperationException($"Map '{definition.Id}' is missing anchor '{id}'.")
            : new Vector2i(anchor.X, anchor.Y);
    }

    private static Schedule ShiftWork(
        int workStart,
        int workEnd,
        int sleepStart,
        int sleepEnd)
    {
        var schedule = Schedule.Create();

        schedule.Entries.Add(
            new ScheduleEntry(null, workStart, workEnd, ScheduleActivity.Work));

        schedule.Entries.Add(
            new ScheduleEntry(null, sleepStart, sleepEnd, ScheduleActivity.Sleep));

        return schedule;
    }

    private static Schedule SleepOnly(int sleepStart, int sleepEnd)
    {
        var schedule = Schedule.Create();

        schedule.Entries.Add(
            new ScheduleEntry(null, sleepStart, sleepEnd, ScheduleActivity.Sleep));

        return schedule;
    }
}
