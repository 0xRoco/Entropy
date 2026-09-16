using Entropy.Content;
using Entropy.Content.Loading;
using Entropy.Engine.World;

namespace Entropy.Simulation;

public class DefinitionRegistry
{
    private readonly Dictionary<string, ItemDefinition> _items = new();
    private readonly Dictionary<string, CreatureDefinition> _creatures = new();
    private readonly Dictionary<string, TerrainDefinition> _terrain = new();
    private readonly Dictionary<string, TilesetDefinition> _tilesets = new();

    private readonly Dictionary<string, ushort> _terrainIndex = new();
    private readonly Dictionary<string, Tile> _terrainTiles = new();
    private readonly Dictionary<ushort, TerrainDefinition> _terrainByIndex = new();
    private readonly List<string?> _terrainSpriteKeys = new();
    
    private readonly Dictionary<string, BuildingTemplate> _buildingTemplates = new();
    private readonly Dictionary<string, WorldObjectDefinition> _worldObjects = new();
    private readonly Dictionary<string, LootTableDefinition> _lootTables = new();

    public void LoadItems(string directory)
    {
        foreach (var def in DefinitionLoader.LoadItems(directory))
        {
            if (!_items.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate item definition ID '{def.Id}' found.");
        }
    }

    public void LoadCreatures(string directory)
    {
        foreach (var def in DefinitionLoader.LoadCreatures(directory))
        {
            if (!_creatures.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate creature definition ID '{def.Id}' found.");
        }
    }
    
    public void LoadBuildingTemplates(string directory)
    {
        foreach (var def in DefinitionLoader.LoadBuildingTemplates(directory))
        {
            if (!_buildingTemplates.TryAdd(def.Id, def))
            {
                throw new InvalidOperationException(
                    $"Duplicate building template definition ID '{def.Id}' found.");
            }
        }
    }

    public void LoadTerrains(string directory)
    {
        ushort index = 1;   // 0 reserved for legacy/default tiles
        _terrainSpriteKeys.Clear();
        _terrainByIndex.Clear();
        foreach (var def in DefinitionLoader.LoadTerrains(directory))
        {
            if (!_terrain.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate terrain definition ID '{def.Id}' found.");

            _terrainIndex[def.Id] = index;
            _terrainTiles[def.Id] = BuildTile(def, index);
            _terrainByIndex[index] = def;
            _terrainSpriteKeys.Add("terrain:" + def.Id);
            index++;
        }
    }

    public void LoadTilesets(string directory)
    {
        foreach (var def in DefinitionLoader.LoadTilesets(directory))
        {
            if (!_tilesets.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate tileset definition ID '{def.Id}' found.");
        }
    }

    public void LoadWorldObjects(string directory)
    {
        foreach (var def in DefinitionLoader.LoadWorldObjects(directory))
        {
            if (!_worldObjects.TryAdd(def.Id, def))
                throw new InvalidOperationException(
                    $"Duplicate world object definition ID '{def.Id}' found.");
        }
    }

    public void LoadLootTables(string directory)
    {
        foreach (var def in DefinitionLoader.LoadLootTables(directory))
        {
            if (!_lootTables.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate loot table ID '{def.Id}' found.");
        }
    }

    public ItemDefinition Item(string id) => _items.TryGetValue(id, out var def)
        ? def
        : throw new KeyNotFoundException($"Item definition with ID '{id}' not found.");

    public CreatureDefinition Creature(string id) => _creatures.TryGetValue(id, out var def)
        ? def
        : throw new KeyNotFoundException($"Creature definition with ID '{id}' not found.");

    public TerrainDefinition Terrain(string id) => _terrain.TryGetValue(id, out var def)
        ? def
        : throw new KeyNotFoundException($"Terrain definition with ID '{id}' not found.");

    public TilesetDefinition Tileset(string id = "default") => _tilesets.TryGetValue(id, out var def)
        ? def
        : throw new KeyNotFoundException($"Tileset definition with ID '{id}' not found.");

    public ushort TerrainIndexOf(string id) => _terrainIndex.TryGetValue(id, out var index)
        ? index
        : throw new KeyNotFoundException($"Terrain definition with ID '{id}' not found.");

    public Tile TileOf(string id) => _terrainTiles.TryGetValue(id, out var tile)
        ? tile
        : throw new KeyNotFoundException($"Terrain definition with ID '{id}' not found.");
    
    public Tile TileForIndex(ushort index)
    {
        if (index == 0 || !_terrainByIndex.TryGetValue(index, out var def))
            throw new KeyNotFoundException($"No terrain definition with index {index}.");
        return BuildTile(def, index);
    }
    
    public BuildingTemplate BuildingTemplate(string id) =>
        _buildingTemplates.TryGetValue(id, out var template)
            ? template
            : throw new KeyNotFoundException(
                $"Building template definition with ID '{id}' not found.");

    public WorldObjectDefinition WorldObject(string id) => _worldObjects.TryGetValue(id, out var def)
        ? def
        : throw new KeyNotFoundException($"World object definition with ID '{id}' not found.");

    public bool TryWorldObject(string id, out WorldObjectDefinition def) =>
        _worldObjects.TryGetValue(id, out def!);

    public LootTableDefinition LootTable(string id) => _lootTables.TryGetValue(id, out var def)
        ? def
        : throw new KeyNotFoundException($"Loot table definition with ID '{id}' not found.");

    public TerrainDefinition TerrainForIndex(ushort index)
    {
        if (index == 0 || !_terrainByIndex.TryGetValue(index, out var def))
            throw new KeyNotFoundException($"No terrain definition with index {index}.");
        return def;
    }

    public IReadOnlyCollection<ItemDefinition> Items => _items.Values;
    public IReadOnlyCollection<CreatureDefinition> Creatures => _creatures.Values;
    public IReadOnlyCollection<TerrainDefinition> Terrains => _terrain.Values;
    public IReadOnlyCollection<TilesetDefinition> Tilesets => _tilesets.Values;
    public IReadOnlyCollection<BuildingTemplate> BuildingTemplates => _buildingTemplates.Values;
    public IReadOnlyCollection<WorldObjectDefinition> WorldObjects => _worldObjects.Values;
    public IReadOnlyCollection<LootTableDefinition> LootTables => _lootTables.Values;

    public IReadOnlyList<string?> TerrainSpriteKeys => _terrainSpriteKeys;

    private static Tile BuildTile(TerrainDefinition def, ushort index)
    {
        var flags = TileFlags.None;
        if (def.HasFlag("road")) flags |= TileFlags.Road;
        if (def.HasFlag("outdoor")) flags |= TileFlags.Outdoor;
        if (def.HasFlag("indoor")) flags |= TileFlags.Indoor;

        return new Tile(def.Symbol, def.Color, def.Background, def.Walkable, def.Opaque,
            index, flags, (ushort)def.MoveCost);
    }
}
