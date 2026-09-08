using System.Text.Json;
using OpenTK.Mathematics;

namespace Entropy.Content.Loading;

public static class DefinitionLoader
{
    public static List<ItemDefinition> LoadItems(string rootDirectory)
    {
        return LoadType(rootDirectory, "ITEM", ParseItem);
    }

    public static List<CreatureDefinition> LoadCreatures(string rootDirectory)
    {
        return LoadType(rootDirectory, "CREATURE", ParseCreature);
    }

    public static List<TerrainDefinition> LoadTerrains(string rootDirectory)
    {
        return LoadType(rootDirectory, "TERRAIN", ParseTerrain);
    }

    public static List<TilesetDefinition> LoadTilesets(string rootDirectory)
    {
        return LoadType(rootDirectory, "TILESET", ParseTileset);
    }

    public static List<BuildingTemplate> LoadBuildingTemplates(string rootDirectory)
    {
        return LoadType(rootDirectory, "BUILDING_TEMPLATE", ParseBuildingTemplate);
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

    private static ItemDefinition ParseItem(JsonElement element, string file)
    {
        var id = GetString(element, "id", file);
        return new ItemDefinition
        {
            Comment = GetStringOr(element, "//", null),
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

    private static CreatureDefinition ParseCreature(JsonElement element, string file)
    {
        var id = GetString(element, "id", file);
        return new CreatureDefinition
        {
            Comment = GetStringOr(element, "//", null),
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

    private static TerrainDefinition ParseTerrain(JsonElement element, string file)
    {
        var id = GetString(element, "id", file);
        return new TerrainDefinition
        {
            Comment = GetStringOr(element, "//", null),
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
        var sprites = new Dictionary<string, Vector2i>();
        if (element.TryGetProperty("sprites", out var spriteObj) && spriteObj.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in spriteObj.EnumerateObject())
            {
                if (entry.Value.ValueKind != JsonValueKind.Array ||
                    entry.Value.GetArrayLength() != 2 ||
                    entry.Value[0].ValueKind != JsonValueKind.Number ||
                    entry.Value[1].ValueKind != JsonValueKind.Number)
                {
                    throw new InvalidOperationException(
                        $"{file}: tileset sprite '{entry.Name}' must be a [col, row] array.");
                }

                sprites[entry.Name] = new Vector2i(
                    entry.Value[0].GetInt32(),
                    entry.Value[1].GetInt32());
            }
        }

        return new TilesetDefinition
        {
            Comment = GetStringOr(element, "//", null),
            Id = GetString(element, "id", file),
            Mode = GetStringOr(element, "mode", "ascii"),
            Atlas = GetStringOr(element, "atlas", string.Empty),
            CellSize = GetIntOr(element, "cell_size", 16),
            Sprites = sprites
        };
    }

    private static BuildingTemplate ParseBuildingTemplate(JsonElement element, string file)
    {
        var id = GetString(element, "id", file);
        var grid = ParseGrid(element, id, file);
        var legend = ParseLegend(element, id, file);
        var anchors = ParseAnchors(element, id, file);

        foreach (var marker in grid.SelectMany(row => row))
        {
            if (!legend.ContainsKey(marker))
            {
                throw new InvalidOperationException(
                    $"{file}: building template '{id}' has grid marker '{marker}' with no legend entry.");
            }
        }

        return new BuildingTemplate
        {
            Comment = GetStringOr(element, "//", null),
            Id = id,
            Name = GetString(element, "name", file),
            Grid = grid,
            Legend = legend,
            Anchors = anchors
        };
    }

    private static List<string> ParseGrid(JsonElement element, string id, string file)
    {
        if (!element.TryGetProperty("grid", out var grid) ||
            grid.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"{file}: building template '{id}' is missing array property 'grid'.");
        }

        var rows = new List<string>();

        foreach (var row in grid.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(row.GetString()))
            {
                throw new InvalidOperationException(
                    $"{file}: building template '{id}' has an empty or invalid grid row.");
            }

            rows.Add(row.GetString()!);
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                $"{file}: building template '{id}' has no grid rows.");
        }

        var width = rows[0].Length;

        if (rows.Any(row => row.Length != width))
        {
            throw new InvalidOperationException(
                $"{file}: building template '{id}' has grid rows with inconsistent widths.");
        }

        return rows;
    }

    private static Dictionary<char, string> ParseLegend(JsonElement element, string id, string file)
    {
        if (!element.TryGetProperty("legend", out var legend) ||
            legend.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"{file}: building template '{id}' is missing object property 'legend'.");
        }

        var result = new Dictionary<char, string>();

        foreach (var property in legend.EnumerateObject())
        {
            if (property.Name.Length != 1 || property.Value.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    $"{file}: building template '{id}' has invalid legend entry '{property.Name}'.");
            }

            result[property.Name[0]] = property.Value.GetString()!;
        }

        return result;
    }

    private static Dictionary<string, Vector2i> ParseAnchors(JsonElement element, string id, string file)
    {
        var result = new Dictionary<string, Vector2i>();

        if (!element.TryGetProperty("anchors", out var anchors))
            return result;

        if (anchors.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"{file}: building template '{id}' has invalid 'anchors'; expected object.");
        }

        foreach (var property in anchors.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array ||
                property.Value.GetArrayLength() != 2)
            {
                throw new InvalidOperationException(
                    $"{file}: building template '{id}' anchor '{property.Name}' must be [x, y].");
            }

            var values = property.Value.EnumerateArray().ToArray();

            if (values[0].ValueKind != JsonValueKind.Number ||
                values[1].ValueKind != JsonValueKind.Number)
            {
                throw new InvalidOperationException(
                    $"{file}: building template '{id}' anchor '{property.Name}' must contain integers.");
            }

            var point = new Vector2i(values[0].GetInt32(), values[1].GetInt32());

            result[property.Name] = point;
        }

        return result;
    }


    private static string GetString(JsonElement e, string name, string file) => e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString()!
            : throw new InvalidOperationException($"Missing or invalid property '{name}' in file '{file}'");

    private static string GetStringOr(JsonElement e, string name, string? fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()!
            : fallback ?? string.Empty;

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
        return ColorNames.TryParse(name, out var color)
            ? color
            : throw new InvalidOperationException($"{file}: item '{id}' has unknown color '{name}'");
    }

    private static Color4 ParseBackground(JsonElement e, string id, string file)
    {
        if (!e.TryGetProperty("background", out var v) || v.ValueKind != JsonValueKind.String)
            return Color4.Black;

        var name = v.GetString()!.ToLowerInvariant();
        return ColorNames.TryParse(name, out var color)
            ? color
            : throw new InvalidOperationException($"{file}: terrain '{id}' has unknown background color '{name}'");
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
}
