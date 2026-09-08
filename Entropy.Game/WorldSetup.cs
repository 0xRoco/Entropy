using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Content;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using Entropy.Game.WorldGen;
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
        TurnProcessor Turns,
        IReadOnlyDictionary<string, BuildingInstance> Buildings);

    public static NewGameResult StartNewGame(
        Rng rng,
        MessageLog log,
        DefinitionRegistry defs,
        int viewRadius)
    {
        var world = new World();
        var turns = new TurnProcessor();

        var block = CityBlockGenerator.Generate(defs);
        var maps = block.Maps;
        var streetMap = maps[block.StreetMapId];

        var visibilities = maps.Maps.ToDictionary(
            pair => pair.Key,
            pair => new VisibilityMap(pair.Value.Width, pair.Value.Height));

        var playerStart = new Vector2i(30, 17);
        var player = EntitySpawner.CreatePlayer(
            world,
            block.StreetMapId,
            defs.Creature("player"),
            playerStart.X,
            playerStart.Y);

        turns.AddActor(player);

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

        turns.AddActor(dana);

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

        turns.AddActor(marcus);

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

        turns.AddActor(priya);

        EntitySpawner.CreateItem(
            world,
            block.StreetMapId,
            defs.Item("iron_sword"),
            28,
            17);

        EntitySpawner.CreateItem(
            world,
            danaApartment.MapId,
            defs.Item("bandage"),
            danaBed.X + 1,
            danaBed.Y,
            count: 2);

        EntitySpawner.CreateItem(
            world,
            store.MapId,
            defs.Item("crackers"),
            store.Anchors["shelf"].X,
            store.Anchors["shelf"].Y,
            count: 3);

        Fov.Compute(
            playerStart,
            viewRadius,
            streetMap,
            visibilities[block.StreetMapId]);

        log.Add($"Seed: {rng.Seed}", Color4.LightGray);
        log.Add("Welcome to the block.", Color4.Red);

        return new NewGameResult(
            streetMap,
            block.StreetMapId,
            maps,
            visibilities,
            world,
            player,
            turns,
            block.Buildings);
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