using System.Text.Json;
using Entropy.Engine.ECS;
using Entropy.Game.Components;

namespace Entropy.Game.Systems;

public sealed record GameSaveData(
    int Version,
    int Seed,
    int ElapsedMinutes,
    string MapId,
    int PlayerX,
    int PlayerY,
    int FacingX,
    int FacingY,
    HealthData Health,
    HungerData Hunger,
    ThirstData Thirst,
    FatigueData Fatigue,
    IReadOnlyList<InventoryItemData> Inventory,
    string? EquippedItemId,
    CharacterData? Character);

public sealed record HealthData(int Current, int Max);
public sealed record HungerData(int Current, int Max, bool Starving);
public sealed record ThirstData(int Current, int Max, bool Parched);
public sealed record FatigueData(int Current, int Max);
public sealed record InventoryItemData(string DefinitionId, int Count);
public sealed record CharacterData(
    string Name,
    string ProfessionId,
    string BackgroundId,
    Dictionary<string, int> Stats,
    IReadOnlyList<string> TraitIds,
    IReadOnlyList<string> SkillIds);

public static class GameSave
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Entropy",
        "save.json");

    public static bool Exists => File.Exists(Path);

    public static void Write(GameSaveData data)
    {
        var directory = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path, JsonSerializer.Serialize(data, Options));
    }

    public static GameSaveData? Read()
    {
        if (!Exists) return null;

        try
        {
            var data = JsonSerializer.Deserialize<GameSaveData>(File.ReadAllText(Path));
            return data is { Version: CurrentVersion } ? data : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static GameSaveData Capture(
        World world,
        Entity player,
        int seed,
        int elapsedMinutes,
        string mapId,
        CharacterData? character = null)
    {
        var position = world.Get<Entropy.Engine.ECS.Components.Position>(player).Value;
        var facing = world.Has<Facing>(player)
            ? world.Get<Facing>(player).Direction
            : new OpenTK.Mathematics.Vector2i(1, 0);
        var health = world.Get<Health>(player);
        var hunger = world.Get<Hunger>(player);
        var thirst = world.Get<Thirst>(player);
        var fatigue = world.Get<Fatigue>(player);
        var inventory = world.Has<Container>(player)
            ? world.Get<Container>(player).Items
                .Where(world.IsAlive)
                .Where(item => world.Has<ItemIdentity>(item))
                .Select(item => new InventoryItemData(
                    world.Get<ItemIdentity>(item).DefinitionId,
                    world.Has<Stackable>(item) ? world.Get<Stackable>(item).Count : 1))
                .ToList()
            : [];

        var equippedItemId = world.Has<Equipped>(player) &&
                             world.IsAlive(world.Get<Equipped>(player).Item) &&
                             world.Has<ItemIdentity>(world.Get<Equipped>(player).Item)
            ? world.Get<ItemIdentity>(world.Get<Equipped>(player).Item).DefinitionId
            : null;

        return new GameSaveData(
            CurrentVersion,
            seed,
            elapsedMinutes,
            mapId,
            (int)position.X,
            (int)position.Y,
            facing.X,
            facing.Y,
            new HealthData(health.Current, health.Max),
            new HungerData(hunger.Current, hunger.Max, hunger.Starving),
            new ThirstData(thirst.Current, thirst.Max, thirst.Parched),
            new FatigueData(fatigue.Current, fatigue.Max),
            inventory,
            equippedItemId,
            character);
    }
}
