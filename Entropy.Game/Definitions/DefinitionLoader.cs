using System.Text.Json;
using OpenTK.Mathematics;

namespace Entropy.Game.Definitions;

public static class DefinitionLoader
{
    public static List<ItemDefinition> LoadItems(string rootDirectory)
    {
        var defs = new List<ItemDefinition>();
        foreach (var file in Directory.GetFiles(rootDirectory, "*.json", SearchOption.AllDirectories))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            var elements = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray().ToList()
                : [root];

            foreach (var element in elements)
            {
                var type = GetString(element, "type", file);
                if (type == "ITEM")
                    defs.Add(ParseItem(element, file));
            }
        }

        return defs;
    }

    public static List<CreatureDefinition> LoadCreatures(string rootDirectory)
    {
        return LoadType<CreatureDefinition>(rootDirectory, "CREATURE", ParseCreature);
    }

    public static List<TerrainDefinition> LoadTerrains(string rootDirectory)
    {
        return LoadType<TerrainDefinition>(rootDirectory, "TERRAIN", ParseTerrain);
    }

    public static List<TilesetDefinition> LoadTilesets(string rootDirectory)
    {
        return LoadType<TilesetDefinition>(rootDirectory, "TILESET", ParseTileset);
    }

    private static List<T> LoadType<T>(string rootDirectory, string typeName,
        Func<JsonElement, string, T> parse)
    {
        var defs = new List<T>();
        foreach (var file in Directory.GetFiles(rootDirectory, "*.json", SearchOption.AllDirectories))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            var elements = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray().ToList()
                : [root];

            foreach (var element in elements)
            {
                var type = GetString(element, "type", file);
                if (type == typeName)
                    defs.Add(parse(element, file));
            }
        }

        return defs;
    }

    private static TerrainDefinition ParseTerrain(JsonElement element, string file)
    {
        var id = GetString(element, "id", file);
        return new TerrainDefinition
        {
            Id = id,
            Name = GetString(element, "name", file),
            Symbol = ParseSymbol(element, file),
            Color = ParseColor(element, id, file),
            Background = ParseBackground(element, id, file),
            Walkable = GetBoolOr(element, "walkable", true),
            Opaque = GetBoolOr(element, "opaque", false),
            MoveCost = GetIntOr(element, "move_cost", 100),
            Flags = ParseTerrainFlags(element, id, file)
        };
    }

    private static TilesetDefinition ParseTileset(JsonElement element, string file)
    {
        return new TilesetDefinition
        {
            Id = GetString(element, "id", file),
            Mode = GetStringOr(element, "mode", "ascii"),
            Atlas = GetStringOr(element, "atlas", string.Empty),
            CellSize = GetIntOr(element, "cell_size", 16)
        };
    }

    private static HashSet<string> ParseTerrainFlags(JsonElement e, string id, string file)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!e.TryGetProperty("flags", out var arr)) return result;

        foreach (var f in arr.EnumerateArray())
        {
            var flag = f.GetString()!;
            if (!ValidTerrainFlags.Contains(flag))
                throw new InvalidOperationException(
                    $"{file}: terrain '{id}' has unknown engine flag '{flag}' (valid: road, outdoor, indoor)");
            result.Add(flag);
        }
        return result;
    }

    private static readonly HashSet<string> ValidTerrainFlags =
        new(StringComparer.OrdinalIgnoreCase) { "road", "outdoor", "indoor" };

    private static Color4 ParseBackground(JsonElement e, string id, string file)
    {
        if (!e.TryGetProperty("background", out var v) || v.ValueKind != JsonValueKind.String)
            return Color4.Black;

        var name = v.GetString()!.ToLowerInvariant();
        return Colors.TryGetValue(name, out var color)
            ? color
            : throw new InvalidOperationException($"{file}: terrain '{id}' has unknown background color '{name}'");
    }

    private static CreatureDefinition ParseCreature(JsonElement element, string file)
    {
        var id = GetString(element, "id", file);
        return new CreatureDefinition
        {
            Id = id,
            Name = GetString(element, "name", file),
            Symbol = ParseSymbol(element, file),
            Color = ParseColor(element, id, file),
            Health = GetIntOr(element, "health", 5),
            Speed = GetIntOr(element, "speed", 100),
            SightRadius = GetIntOr(element, "sight", 5),
            SmellRadius = GetIntOr(element, "smell", 0),
            Behavior = GetStringOr(element, "behavior", "wander"),
            Hostile = GetBoolOr(element, "hostile", false)
        };
    }

    private static ItemDefinition ParseItem(JsonElement element, string file)
    {
        var type = GetString(element, "type", file);

        if (type != "ITEM")
            throw new InvalidOperationException($"{file}: unknown definition type '{type}' - only 'ITEM' is supported");

        var id = GetString(element, "id", file);

        return new ItemDefinition
        {
            Id = id,
            Name = GetString(element, "name", file),
            Symbol = ParseSymbol(element, file),
            Color = ParseColor(element, id, file),
            Description = GetStringOr(element, "description", string.Empty),
            Category = GetStringOr(element, "category", string.Empty),
            Materials = ParseMaterials(element, id, file),
            Flags = ParseFlags(element),
            Weight = GetIntOr(element, "weight", 0),
            Stackable = GetBoolOr(element, "stackable", false),
            MaxStack = GetIntOr(element, "max_stack", 10),
            Effects = ParseEffects(element, id, file)
        };
    }

    private static string GetString(JsonElement e, string name, string file) => e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString()!
            : throw new InvalidOperationException($"Missing or invalid property '{name}' in file '{file}'");

    private static string GetStringOr(JsonElement e, string name, string fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()!
            : fallback;

    private static int GetIntOr(JsonElement e, string name, int fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : fallback;

    private static bool GetBoolOr(JsonElement e, string name, bool fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? v.GetBoolean()
            : fallback;

    private static char ParseSymbol(JsonElement e, string file)
    {
        var s = GetString(e, "symbol", file);
        return s.Length == 1
            ? s[0]
            : throw new InvalidOperationException($"{file}: 'symbol' must be a single character, got \"{s}\"");
    }

    private static Color4 ParseColor(JsonElement e, string id, string file)
    {
        var name = GetString(e, "color", file).ToLowerInvariant();
        return Colors.TryGetValue(name, out var color)
            ? color
            : throw new InvalidOperationException($"{file}: item '{id}' has unknown color '{name}'");
    }

    private static List<Material> ParseMaterials(JsonElement e, string id, string file)
    {
        var result = new List<Material>();
        if (!e.TryGetProperty("material", out var arr)) return result;

        foreach (var m in arr.EnumerateArray())
        {
            var name = m.GetString()!;
            if (!Enum.TryParse<Material>(name, ignoreCase: true, out var material))
                throw new InvalidOperationException(
                    $"{file}: item '{id}' has unknown material '{name}'");
            result.Add(material);
        }

        return result;
    }

    private static HashSet<string> ParseFlags(JsonElement e)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!e.TryGetProperty("flags", out var arr)) return result;

        foreach (var f in arr.EnumerateArray())
            result.Add(f.GetString()!);
        return result;
    }

    private static List<ItemEffect> ParseEffects(JsonElement e, string id, string file)
    {
        var result = new List<ItemEffect>();
        if (!e.TryGetProperty("effects", out var arr)) return result;

        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"{file}: item '{id}' has a non-object entry in 'effects'");

            var kind = el.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String
                ? k.GetString()!
                : throw new InvalidOperationException($"{file}: item '{id}' has an effect missing 'kind'");

            switch (kind)
            {
                case "heal":
                    result.Add(new ItemEffect.Heal(GetRequiredInt(el, "amount", file)));
                    break;

                case "damage":
                    result.Add(new ItemEffect.Damage(GetRequiredInt(el, "amount", file)));
                    break;

                case "nourish":
                    result.Add(new ItemEffect.Nourish(GetRequiredInt(el, "amount", file)));
                    break;

                case "hydrate":
                    result.Add(new ItemEffect.Hydrate(GetRequiredInt(el, "amount", file)));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"{file}: item '{id}' has unknown effect kind '{kind}'");
            }
        }

        return result;
    }

    private static int GetRequiredInt(JsonElement e, string name, string file) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : throw new InvalidOperationException($"{file}: missing or invalid '{name}'");

    private static readonly Dictionary<string, Color4> Colors = new()
    {
        ["white"] = Color4.White,
        ["black"] = Color4.Black,
        ["gray"] = Color4.Gray,
        ["light_gray"] = Color4.LightGray,
        ["dark_gray"] = Color4.DarkGray,
        ["red"] = Color4.Red,
        ["dark_red"] = new Color4(0.5f, 0f, 0f, 1f),
        ["green"] = Color4.Green,
        ["dark_green"] = new Color4(0f, 0.5f, 0f, 1f),
        ["blue"] = Color4.Blue,
        ["light_blue"] = new Color4(0.5f, 0.7f, 1f, 1f),
        ["yellow"] = Color4.Yellow,
        ["light_yellow"] = Color4.LightYellow,
        ["pink"] = Color4.Pink,
        ["orange"] = Color4.Orange,
        ["brown"] = new Color4(0.55f, 0.4f, 0.25f, 1f),
        ["cyan"] = Color4.Cyan,
        ["magenta"] = Color4.Magenta
    };
}