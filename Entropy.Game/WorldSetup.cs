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

        var block = CityBlockGenerator.Generate(defs);
        var maps = block.Maps;
        var streetMap = maps[block.StreetMapId];

        var visibilities = maps.Maps.ToDictionary(
            pair => pair.Key,
            pair => new VisibilityMap(pair.Value.Width, pair.Value.Height));

        var playerStart = new Vector2i(30, 20);
        var player = EntitySpawner.CreatePlayer(
            world,
            block.StreetMapId,
            defs.Creature("player"),
            playerStart.X,
            playerStart.Y);

        scheduler.Add(player);

        foreach (var building in block.Buildings.Values)
        {
            foreach (var (anchorName, tile) in building.Anchors)
            {
                if (!defs.TryWorldObject(anchorName, out var objectDef) &&
                    !defs.TryWorldObject(anchorName.TrimEnd("0123456789".ToCharArray()), out objectDef))
                    continue;

                var spawned = EntitySpawner.CreateWorldObject(
                    world,
                    building.MapId,
                    objectDef,
                    tile.X,
                    tile.Y);

                foreach (var itemId in objectDef.StarterItems)
                    EntitySpawner.SpawnIntoContainer(world, spawned, defs.Item(itemId));

                if (objectDef.LootTableId is { Length: > 0 } lootTableId)
                    LootSystem.Generate(world, defs, rng, spawned, defs.LootTable(lootTableId));
            }
        }

        var civilian = defs.Creature("human_civilian");

        var store = block.Buildings["corner_store"];
        var danaApartment = block.Buildings["apartment_dana"];
        var marcusApartment = block.Buildings["apartment_marcus"];

        var danaHome = danaApartment.Anchors["home"];
        var danaBed = danaApartment.Anchors["bed"];
        var storeWork = store.Anchors["work"];

        var dana = EntitySpawner.CreateHuman(
            world,
            danaApartment.MapId,
            civilian,
            danaBed.X,
            danaBed.Y,
            "Dana");

        dana.With(world, new Home
        {
            MapId = danaApartment.MapId,
            Tile = danaHome
        });

        dana.With(world, new Workplace
        {
            MapId = store.MapId,
            Tile = storeWork
        });

        dana.With(world, ShiftWork(
            workStart: 8 * 60,
            workEnd: 17 * 60,
            sleepStart: 22 * 60,
            sleepEnd: 7 * 60));

        scheduler.Add(dana);

        var marcusHome = marcusApartment.Anchors["home"];
        var marcusBed = marcusApartment.Anchors["bed"];

        var marcus = EntitySpawner.CreateHuman(
            world,
            marcusApartment.MapId,
            civilian,
            marcusBed.X,
            marcusBed.Y,
            "Marcus");

        marcus.With(world, new Home
        {
            MapId = marcusApartment.MapId,
            Tile = marcusHome
        });

        marcus.With(world, new Workplace
        {
            MapId = store.MapId,
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
            danaApartment.MapId,
            civilian,
            danaHome.X - 1,
            danaHome.Y,
            "Priya");

        priya.With(world, new Home
        {
            MapId = danaApartment.MapId,
            Tile = danaHome
        });

        priya.With(world, SleepOnly(
            sleepStart: 23 * 60,
            sleepEnd: 7 * 60));

        scheduler.Add(priya);

        var house = block.Buildings["neighborhood_house"];
        var keyTile = house.Anchors["home"];
        EntitySpawner.CreateItem(
            world,
            house.MapId,
            defs.Item("key_pharmacy"),
            keyTile.X,
            keyTile.Y);

        var pharmacy = block.Buildings["neighborhood_pharmacy"];
        var pharmacyShelf = pharmacy.Anchors["shelf1"];
        EntitySpawner.CreateItem(
            world,
            pharmacy.MapId,
            defs.Item("first_aid_kit"),
            pharmacyShelf.X,
            pharmacyShelf.Y);

        var hardware = block.Buildings["neighborhood_hardware"];
        var hardwareShelf = hardware.Anchors["shelf1"];
        EntitySpawner.CreateItem(
            world,
            hardware.MapId,
            defs.Item("baseball_bat"),
            hardwareShelf.X,
            hardwareShelf.Y);

        var zombie = EntitySpawner.CreateHuman(
            world,
            block.StreetMapId,
            defs.Creature("zombie"),
            108,
            20,
            "Zombie");
        scheduler.Add(zombie);

        Fov.Compute(
            playerStart,
            viewRadius,
            streetMap,
            visibilities[block.StreetMapId]);

        log.Add($"Seed: {rng.Seed}", Color4.LightGray);
        log.Add("Welcome to the block.", Color4.Red);

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
            streetMap,
            block.StreetMapId,
            maps,
            visibilities,
            world,
            player,
            block.Buildings,
            simulation);
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
