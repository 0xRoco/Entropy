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
    public record NewGameResult(
        TileMap Map,
        World World,
        Entity Player,
        VisibilityMap Visibility,
        TurnProcessor Turns);

    public static NewGameResult StartNewGame(Rng rng, MessageLog log, DefinitionRegistry defs, int viewRadius)
    {
        var world = new World();
        var turns = new TurnProcessor();

        var (map, roomCenters) = MapGenerator.Generate(60, 40, rng, 25);

        var spawn = roomCenters[0];
        var player = EntitySpawner.CreatePlayer(world, spawn.X, spawn.Y);
        turns.AddActor(player);

        var human = EntitySpawner.CreateHuman(world, spawn.X + 1, spawn.Y);
        turns.AddActor(human);
        
        EntitySpawner.CreateItem(world, defs.Item("iron_sword"), spawn.X - 1, spawn.Y + 1);
        EntitySpawner.CreateItem(world, defs.Item("bandage"), spawn.X + 2, spawn.Y + 1, count: 2);
        EntitySpawner.CreateItem(world, defs.Item("crackers"), spawn.X, spawn.Y + 1, count: 3);

        foreach (var roomCenter in roomCenters.Skip(1))
        {
            var zombie = EntitySpawner.CreateZombie(world, roomCenter.X + 1, roomCenter.Y);
            turns.AddActor(zombie);
        }

        var visibility = new VisibilityMap(map.Width, map.Height);
        var playerPos = world.Get<Position>(player).Value;
        Fov.Compute(new Vector2i((int)playerPos.X, (int)playerPos.Y), viewRadius, map, visibility);

        log.Add($"Seed: {rng.Seed}", Color4.LightGray);
        log.Add("Welcome to Entropy!", Color4.Red);

        return new NewGameResult(map, world, player, visibility, turns);
    }

}