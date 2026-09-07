using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Definitions;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Mathematics;

namespace Entropy.Game;

public class WorldSetup
{
    private const string WorldMapId = "world";

    public record NewGameResult(
        TileMap Map,
        string MapId,
        MapGraph Maps,
        World World,
        Entity Player,
        VisibilityMap Visibility,
        TurnProcessor Turns);

    public static NewGameResult StartNewGame(Rng rng, MessageLog log, DefinitionRegistry defs, int viewRadius)
    {
        var world = new World();
        var turns = new TurnProcessor();

        var (map, roomCenters) = MapGenerator.Generate(60, 40, rng, 25);

        var maps = new MapGraph();
        maps.AddMap(WorldMapId, map);

        var spawn = roomCenters[0];
        var player = EntitySpawner.CreatePlayer(world, WorldMapId, spawn.X, spawn.Y);
        turns.AddActor(player);

        var civilian = defs.Creature("human_civilian");

        var store = roomCenters[1];
        var homeA = roomCenters[2];
        var homeB = roomCenters.Count > 3 ? roomCenters[3] : roomCenters[2];

        var dana = EntitySpawner.CreateHuman(
            world, WorldMapId, civilian, spawn.X + 1, spawn.Y, "Dana");
        dana.With(world, new Home { Tile = homeA });
        dana.With(world, new Workplace { Tile = store });
        dana.With(world, ShiftWork(8 * 60, 17 * 60, 22 * 60, 7 * 60));
        turns.AddActor(dana);

        var marcus = EntitySpawner.CreateHuman(
            world, WorldMapId, civilian, spawn.X + 2, spawn.Y + 1, "Marcus");
        marcus.With(world, new Home { Tile = homeB });
        marcus.With(world, new Workplace { Tile = store });
        marcus.With(world, ShiftWork(22 * 60, 6 * 60, 8 * 60, 16 * 60));
        turns.AddActor(marcus);

        var priya = EntitySpawner.CreateHuman(
            world, WorldMapId, civilian, spawn.X - 1, spawn.Y + 1, "Priya");
        priya.With(world, new Home { Tile = homeA });
        priya.With(world, SleepOnly(23 * 60, 7 * 60));
        turns.AddActor(priya);

        EntitySpawner.CreateItem(
            world, WorldMapId, defs.Item("iron_sword"), spawn.X - 1, spawn.Y + 1);
        EntitySpawner.CreateItem(
            world, WorldMapId, defs.Item("bandage"), spawn.X + 2, spawn.Y + 1, count: 2);
        EntitySpawner.CreateItem(
            world, WorldMapId, defs.Item("crackers"), spawn.X, spawn.Y + 1, count: 3);

        var visibility = new VisibilityMap(map.Width, map.Height);
        var playerPos = world.Get<Position>(player).Value;
        Fov.Compute(new Vector2i((int)playerPos.X, (int)playerPos.Y), viewRadius, map, visibility);

        log.Add($"Seed: {rng.Seed}", Color4.LightGray);
        log.Add("Welcome to Entropy!", Color4.Red);

        return new NewGameResult(
            map,
            WorldMapId,
            maps,
            world,
            player,
            visibility,
            turns);
    }

    private static Schedule ShiftWork(int workStart, int workEnd, int sleepStart, int sleepEnd)
    {
        var schedule = Schedule.Create();
        schedule.Entries.Add(new ScheduleEntry(null, workStart, workEnd, ScheduleActivity.Work));
        schedule.Entries.Add(new ScheduleEntry(null, sleepStart, sleepEnd, ScheduleActivity.Sleep));
        return schedule;
    }

    private static Schedule SleepOnly(int sleepStart, int sleepEnd)
    {
        var schedule = Schedule.Create();
        schedule.Entries.Add(new ScheduleEntry(null, sleepStart, sleepEnd, ScheduleActivity.Sleep));
        return schedule;
    }
}