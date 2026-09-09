namespace Entropy.Content.Validation;

public static class DefinitionValidator
{
    public static List<string> Validate(
        IReadOnlyCollection<ItemDefinition> items,
        IReadOnlyCollection<CreatureDefinition> creatures,
        IReadOnlyCollection<TerrainDefinition> terrains,
        IReadOnlyCollection<TilesetDefinition> tilesets,
        IReadOnlyCollection<BuildingTemplate> buildings,
        IReadOnlyCollection<WorldObjectDefinition> worldObjects)
    {
        var errors = new List<string>();

        errors.AddRange(ValidateItems(items));
        errors.AddRange(ValidateCreatures(creatures));
        errors.AddRange(ValidateTerrains(terrains));
        errors.AddRange(ValidateTilesets(tilesets,
            ItemIds(items), CreatureIds(creatures), TerrainIds(terrains), WorldObjectIds(worldObjects)));
        errors.AddRange(ValidateBuildingTemplates(buildings, TerrainIds(terrains)));
        errors.AddRange(ValidateWorldObjects(worldObjects, ItemIds(items)));

        return errors;
    }

    public static List<string> ValidateItems(IEnumerable<ItemDefinition> items)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in items)
        {
            if (string.IsNullOrWhiteSpace(def.Id))
                errors.Add("Item with empty id.");
            else if (!seenIds.Add(def.Id))
                errors.Add($"Duplicate item id '{def.Id}'.");

            if (string.IsNullOrWhiteSpace(def.Name))
                errors.Add($"Item '{def.Id}' has no name.");

            if (ColorNames.NameOf(def.Color) is null)
                errors.Add($"Item '{def.Id}' has a color with no palette name.");

            foreach (var material in def.Materials)
            {
                if (!Enum.IsDefined(material))
                    errors.Add($"Item '{def.Id}' has invalid material '{material}'.");
            }

            if (def.Stackable && def.MaxStack <= 0)
                errors.Add($"Item '{def.Id}' is stackable but max_stack is {def.MaxStack}.");

            foreach (var effect in def.Effects)
            {
                switch (effect)
                {
                    case ItemEffect.Heal heal when heal.Amount <= 0:
                        errors.Add($"Item '{def.Id}' has a heal effect with amount {heal.Amount}.");
                        break;
                    case ItemEffect.Damage damage when damage.Amount <= 0:
                        errors.Add($"Item '{def.Id}' has a damage effect with amount {damage.Amount}.");
                        break;
                }
            }
        }

        return errors;
    }

    public static List<string> ValidateCreatures(IEnumerable<CreatureDefinition> creatures)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in creatures)
        {
            if (string.IsNullOrWhiteSpace(def.Id))
                errors.Add("Creature with empty id.");
            else if (!seenIds.Add(def.Id))
                errors.Add($"Duplicate creature id '{def.Id}'.");

            if (string.IsNullOrWhiteSpace(def.Name))
                errors.Add($"Creature '{def.Id}' has no name.");

            if (ColorNames.NameOf(def.Color) is null)
                errors.Add($"Creature '{def.Id}' has a color with no palette name.");

            if (def.Health <= 0)
                errors.Add($"Creature '{def.Id}' has health {def.Health}.");
        }

        return errors;
    }

    public static List<string> ValidateTerrains(IEnumerable<TerrainDefinition> terrains)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in terrains)
        {
            if (string.IsNullOrWhiteSpace(def.Id))
                errors.Add("Terrain with empty id.");
            else if (!seenIds.Add(def.Id))
                errors.Add($"Duplicate terrain id '{def.Id}'.");

            if (string.IsNullOrWhiteSpace(def.Name))
                errors.Add($"Terrain '{def.Id}' has no name.");

            if (ColorNames.NameOf(def.Color) is null)
                errors.Add($"Terrain '{def.Id}' has a color with no palette name.");

            if (ColorNames.NameOf(def.Background) is null)
                errors.Add($"Terrain '{def.Id}' has a background color with no palette name.");

            if (def.MoveCost <= 0)
                errors.Add($"Terrain '{def.Id}' has move_cost {def.MoveCost}.");
        }

        return errors;
    }

    public static List<string> ValidateTilesets(
        IEnumerable<TilesetDefinition> tilesets,
        IReadOnlyCollection<string>? itemIds = null,
        IReadOnlyCollection<string>? creatureIds = null,
        IReadOnlyCollection<string>? terrainIds = null,
        IReadOnlyCollection<string>? worldObjectIds = null)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in tilesets)
        {
            if (string.IsNullOrWhiteSpace(def.Id))
                errors.Add("Tileset with empty id.");
            else if (!seenIds.Add(def.Id))
                errors.Add($"Duplicate tileset id '{def.Id}'.");

            if (def.Mode is not ("ascii" or "art"))
                errors.Add($"Tileset '{def.Id}' has unknown mode '{def.Mode}'.");

            if (def.Mode == "art" && string.IsNullOrWhiteSpace(def.Atlas))
                errors.Add($"Tileset '{def.Id}' is art mode but has no atlas path.");

            errors.AddRange(ValidateSpriteKeys(def, itemIds, creatureIds, terrainIds, worldObjectIds));
        }

        return errors;
    }
    
    public static List<string> ValidateBuildingTemplates(
        IEnumerable<BuildingTemplate> buildings,
        IReadOnlyCollection<string>? terrainIds = null)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in buildings)
        {
            if (string.IsNullOrWhiteSpace(def.Id))
                errors.Add("Building template with empty id.");
            else if (!seenIds.Add(def.Id))
                errors.Add($"Duplicate building template id '{def.Id}'.");

            if (def.Grid.Count == 0)
                errors.Add($"Building template '{def.Id}' has no grid.");

            if (def.Grid.Select(r => r.Length).Distinct().Count() > 1)
                errors.Add($"Building template '{def.Id}' has grid rows with inconsistent widths.");

            errors.AddRange(ValidateBuildingLegend(def, terrainIds));
            errors.AddRange(ValidateBuildingAnchors(def));
        }

        return errors;
    }

    private static List<string> ValidateBuildingLegend(
        BuildingTemplate def,
        IReadOnlyCollection<string>? terrainIds)
    {
        var errors = new List<string>();

        foreach (var (marker, terrainId) in def.Legend)
        {
            if (terrainIds?.Contains(terrainId, StringComparer.OrdinalIgnoreCase) == false)
                errors.Add($"Building template '{def.Id}' legend maps '{marker}' to unknown terrain id '{terrainId}'.");
        }

        foreach (var row in def.Grid)
        {
            foreach (var marker in row)
            {
                if (!def.Legend.ContainsKey(marker))
                    errors.Add($"Building template '{def.Id}' grid uses marker '{marker}' with no legend entry.");
            }
        }

        return errors;
    }

    private static List<string> ValidateBuildingAnchors(BuildingTemplate def)
    {
        var errors = new List<string>();
        if (def.Grid.Count == 0) return errors;

        var width = def.Grid.Max(r => r.Length);

        foreach (var (name, pos) in def.Anchors)
        {
            if (pos.X < 0 || pos.Y < 0 || pos.X >= width || pos.Y >= def.Grid.Count)
                errors.Add($"Building template '{def.Id}' anchor '{name}' ({pos.X}, {pos.Y}) is outside the grid.");
        }

        return errors;
    }

    private static List<string> ValidateSpriteKeys(
        TilesetDefinition def,
        IReadOnlyCollection<string>? itemIds,
        IReadOnlyCollection<string>? creatureIds,
        IReadOnlyCollection<string>? terrainIds,
        IReadOnlyCollection<string>? worldObjectIds = null)
    {
        var errors = new List<string>();

        foreach (var (key, cell) in def.Sprites)
        {
            var prefixEnd = key.IndexOf(':');
            if (prefixEnd <= 0 || prefixEnd == key.Length - 1)
            {
                errors.Add($"Tileset '{def.Id}' sprite key '{key}' has no <kind>:<id> prefix.");
                continue;
            }

            var kind = key[..prefixEnd];
            var id = key[(prefixEnd + 1)..];

            if (kind is not ("item" or "creature" or "terrain" or "furniture"))
            {
                errors.Add($"Tileset '{def.Id}' sprite key '{key}' has unknown kind '{kind}'.");
                continue;
            }

            bool? known = kind switch
            {
                "item" => itemIds?.Contains(id, StringComparer.OrdinalIgnoreCase),
                "creature" => creatureIds?.Contains(id, StringComparer.OrdinalIgnoreCase),
                "furniture" => worldObjectIds?.Contains(id, StringComparer.OrdinalIgnoreCase),
                _ => terrainIds?.Contains(id, StringComparer.OrdinalIgnoreCase)
            };

            if (known == false)
                errors.Add($"Tileset '{def.Id}' sprite key '{key}' references unknown {kind} id '{id}'.");

            if (cell.X < 0 || cell.Y < 0)
                errors.Add($"Tileset '{def.Id}' sprite key '{key}' has negative cell coordinates.");
        }

        return errors;
    }

    public static List<string> ValidateWorldObjects(
        IEnumerable<WorldObjectDefinition> worldObjects,
        IReadOnlyCollection<string>? itemIds = null)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in worldObjects)
        {
            if (string.IsNullOrWhiteSpace(def.Id))
                errors.Add("World object with empty id.");
            else if (!seenIds.Add(def.Id))
                errors.Add($"Duplicate world object id '{def.Id}'.");

            if (string.IsNullOrWhiteSpace(def.Name))
                errors.Add($"World object '{def.Id}' has no name.");

            if (ColorNames.NameOf(def.Color) is null)
                errors.Add($"World object '{def.Id}' has a color with no palette name.");

            if (def.ContainerSlots < 0)
                errors.Add($"World object '{def.Id}' has negative container_slots.");

            foreach (var itemId in def.StarterItems)
            {
                if (itemIds?.Contains(itemId, StringComparer.OrdinalIgnoreCase) == false)
                    errors.Add($"World object '{def.Id}' starts with unknown item id '{itemId}'.");
            }
        }

        return errors;
    }

    private static IReadOnlyCollection<string> ItemIds(IEnumerable<ItemDefinition> items) =>
        items.Select(d => d.Id).ToList();

    private static IReadOnlyCollection<string> WorldObjectIds(IEnumerable<WorldObjectDefinition> worldObjects) =>
        worldObjects.Select(d => d.Id).ToList();

    private static IReadOnlyCollection<string> CreatureIds(IEnumerable<CreatureDefinition> creatures) =>
        creatures.Select(d => d.Id).ToList();

    private static IReadOnlyCollection<string> TerrainIds(IEnumerable<TerrainDefinition> terrains) =>
        terrains.Select(d => d.Id).ToList();
}
