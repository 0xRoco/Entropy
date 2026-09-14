using System.Text.Json;
using OpenTK.Mathematics;

namespace Entropy.Content.Writing;

public static class DefinitionWriter
{
    private static readonly JsonWriterOptions Options = new() { Indented = true };

    public static void WriteItems(string path, IReadOnlyCollection<ItemDefinition> items)
    {
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, Options);

        writer.WriteStartArray();

        foreach (var def in items)
        {
            writer.WriteStartObject();

            WriteComment(writer, def.Comment);
            writer.WriteString("type", "ITEM");
            writer.WriteString("id", def.Id);
            writer.WriteString("name", def.Name);
            writer.WriteString("symbol", def.Symbol.ToString());
            writer.WriteString("color", ColorName(def.Color, def.Id));

            if (def.Description.Length > 0) writer.WriteString("description", def.Description);
            if (def.Category.Length > 0) writer.WriteString("category", def.Category);

            WriteMaterials(writer, def.Materials);
            WriteFlags(writer, def.Flags);
            if (def.Weight > 0) writer.WriteNumber("weight", def.Weight);
            if (def.PriceCents > 0) writer.WriteNumber("price_cents", def.PriceCents);

            if (def.Stackable)
            {
                writer.WriteBoolean("stackable", true);
                writer.WriteNumber("max_stack", def.MaxStack);
            }

            WriteEffects(writer, def.Effects);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteCreatures(string path, IReadOnlyCollection<CreatureDefinition> creatures)
    {
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, Options);

        writer.WriteStartArray();

        foreach (var def in creatures)
        {
            writer.WriteStartObject();

            WriteComment(writer, def.Comment);
            writer.WriteString("type", "CREATURE");
            writer.WriteString("id", def.Id);
            writer.WriteString("name", def.Name);
            writer.WriteString("symbol", def.Symbol.ToString());
            writer.WriteString("color", ColorName(def.Color, def.Id));
            writer.WriteNumber("health", def.Health);
            writer.WriteNumber("speed", def.Speed);
            writer.WriteNumber("sight", def.SightRadius);
            writer.WriteNumber("smell", def.SmellRadius);
            writer.WriteString("behavior", def.Behavior);
            if (def.Hostile) writer.WriteBoolean("hostile", true);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteTerrains(string path, IReadOnlyCollection<TerrainDefinition> terrains)
    {
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, Options);

        writer.WriteStartArray();

        foreach (var def in terrains)
        {
            writer.WriteStartObject();

            WriteComment(writer, def.Comment);
            writer.WriteString("type", "TERRAIN");
            writer.WriteString("id", def.Id);
            writer.WriteString("name", def.Name);
            writer.WriteString("symbol", def.Symbol.ToString());
            writer.WriteString("color", ColorName(def.Color, def.Id));
            writer.WriteString("background", ColorName(def.Background, def.Id));

            if (def.Description.Length > 0) writer.WriteString("description", def.Description);
            if (def.Category.Length > 0) writer.WriteString("category", def.Category);

            WriteTerrainFlags(writer, def.Flags);

            writer.WriteBoolean("walkable", def.Walkable);
            writer.WriteBoolean("opaque", def.Opaque);
            writer.WriteNumber("move_cost", def.MoveCost);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteTilesets(string path, IReadOnlyCollection<TilesetDefinition> tilesets)
    {
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, Options);

        writer.WriteStartArray();

        foreach (var def in tilesets)
        {
            writer.WriteStartObject();

            WriteComment(writer, def.Comment);
            writer.WriteString("type", "TILESET");
            writer.WriteString("id", def.Id);
            writer.WriteString("mode", def.Mode);
            writer.WriteString("atlas", def.Atlas);
            writer.WriteNumber("cell_size", def.CellSize);

            if (def.Sprites.Count > 0)
            {
                writer.WritePropertyName("sprites");
                writer.WriteStartObject();
                foreach (var (key, cell) in def.Sprites)
                {
                    writer.WritePropertyName(key);
                    writer.WriteStartArray();
                    writer.WriteNumberValue(cell.X);
                    writer.WriteNumberValue(cell.Y);
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteBuildingTemplates(string path, IReadOnlyCollection<BuildingTemplate> templates)
    {
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, Options);

        writer.WriteStartArray();

        foreach (var def in templates)
        {
            writer.WriteStartObject();

            WriteComment(writer, def.Comment);
            writer.WriteString("type", "BUILDING_TEMPLATE");
            writer.WriteString("id", def.Id);
            writer.WriteString("name", def.Name);

            writer.WritePropertyName("grid");
            writer.WriteStartArray();
            foreach (var row in def.Grid) writer.WriteStringValue(row);
            writer.WriteEndArray();

            writer.WritePropertyName("legend");
            writer.WriteStartObject();
            foreach (var (marker, terrainId) in def.Legend)
                writer.WriteString(marker.ToString(), terrainId);
            writer.WriteEndObject();

            writer.WritePropertyName("anchors");
            writer.WriteStartObject();
            foreach (var (name, point) in def.Anchors)
            {
                writer.WritePropertyName(name);
                writer.WriteStartArray();
                writer.WriteNumberValue(point.X);
                writer.WriteNumberValue(point.Y);
                writer.WriteEndArray();
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }


    public static void WriteWorldObjects(string path, IReadOnlyCollection<WorldObjectDefinition> worldObjects)
    {
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, Options);

        writer.WriteStartArray();

        foreach (var def in worldObjects)
        {
            writer.WriteStartObject();

            WriteComment(writer, def.Comment);
            writer.WriteString("type", "WORLD_OBJECT");
            writer.WriteString("id", def.Id);
            writer.WriteString("name", def.Name);
            writer.WriteString("symbol", def.Symbol.ToString());
            writer.WriteString("color", ColorName(def.Color, def.Id));

            if (def.Description.Length > 0) writer.WriteString("description", def.Description);

            WriteFlags(writer, def.Flags);
            if (def.ContainerSlots > 0) writer.WriteNumber("container_slots", def.ContainerSlots);
            if (!string.IsNullOrWhiteSpace(def.LootTableId))
                writer.WriteString("loot_table", def.LootTableId);
            if (def.Locked)
            {
                writer.WriteBoolean("locked", true);
                if (!string.IsNullOrWhiteSpace(def.RequiredKeyFlag))
                    writer.WriteString("required_key_flag", def.RequiredKeyFlag);
                if (!string.IsNullOrWhiteSpace(def.RequiredToolFlag))
                    writer.WriteString("required_tool_flag", def.RequiredToolFlag);
            }

            if (def.StarterItems.Count > 0)
            {
                writer.WritePropertyName("starter_items");
                writer.WriteStartArray();
                foreach (var itemId in def.StarterItems)
                    writer.WriteStringValue(itemId);
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteComment(Utf8JsonWriter writer, string? comment)
    {
        if (!string.IsNullOrEmpty(comment))
            writer.WriteString("//", comment);
    }

    private static void WriteMaterials(Utf8JsonWriter writer, List<Material> materials)
    {
        if (materials.Count == 0) return;

        writer.WritePropertyName("material");
        writer.WriteStartArray();
        foreach (var material in materials)
            writer.WriteStringValue(material.ToString());
        writer.WriteEndArray();
    }

    private static void WriteFlags(Utf8JsonWriter writer, HashSet<string> flags)
    {
        if (flags.Count == 0) return;

        writer.WritePropertyName("flags");
        writer.WriteStartArray();
        foreach (var flag in flags)
            writer.WriteStringValue(flag);
        writer.WriteEndArray();
    }

    private static void WriteTerrainFlags(Utf8JsonWriter writer, HashSet<string> flags)
    {
        if (flags.Count == 0) return;

        writer.WritePropertyName("flags");
        writer.WriteStartArray();
        foreach (var flag in flags)
            writer.WriteStringValue(flag);
        writer.WriteEndArray();
    }

    private static void WriteEffects(Utf8JsonWriter writer, List<ItemEffect> effects)
    {
        if (effects.Count == 0) return;

        writer.WritePropertyName("effects");
        writer.WriteStartArray();

        foreach (var effect in effects)
        {
            writer.WriteStartObject();
            switch (effect)
            {
                case ItemEffect.Heal heal:
                    writer.WriteString("kind", "heal");
                    writer.WriteNumber("amount", heal.Amount);
                    break;
                case ItemEffect.Damage damage:
                    writer.WriteString("kind", "damage");
                    writer.WriteNumber("amount", damage.Amount);
                    break;
                case ItemEffect.Nourish nourish:
                    writer.WriteString("kind", "nourish");
                    writer.WriteNumber("amount", nourish.Amount);
                    break;
                case ItemEffect.Hydrate hydrate:
                    writer.WriteString("kind", "hydrate");
                    writer.WriteNumber("amount", hydrate.Amount);
                    break;
            }
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static string ColorName(Color4 color, string defId) =>
        ColorNames.NameOf(color)
        ?? throw new InvalidOperationException(
            $"Definition '{defId}' has a color with no name, pick a named color from the palette.");
}
