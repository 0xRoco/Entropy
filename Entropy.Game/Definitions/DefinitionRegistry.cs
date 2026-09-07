using Entropy.Engine.World;

namespace Entropy.Game.Definitions;

public class DefinitionRegistry
{
    private readonly Dictionary<string, ItemDefinition> _items = new();
    private readonly Dictionary<string, CreatureDefinition> _creatures = new();
    private readonly Dictionary<string, TerrainDefinition> _terrain = new();
    private readonly Dictionary<string, TilesetDefinition> _tilesets = new();

    private readonly Dictionary<string, ushort> _terrainIndex = new();
    private readonly Dictionary<string, Tile> _terrainTiles = new();

    public void LoadItems(string directory)
    {
        foreach (var def in DefinitionLoader.LoadItems(directory))
        {
            if (!_items.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate item definition ID '{def.Id}' found.");
        }

        Console.WriteLine($"[DefinitionRegistry] Loaded {_items.Count} item definitions.");
    }

    public void LoadCreatures(string directory)
    {
        foreach (var def in DefinitionLoader.LoadCreatures(directory))
        {
            if (!_creatures.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate creature definition ID '{def.Id}' found.");
        }
        Console.WriteLine($"[DefinitionRegistry] Loaded {_creatures.Count} creature definitions.");
    }

    public void LoadTerrains(string directory)
    {
        ushort index = 1;   // 0 reserved for legacy/default tiles
        foreach (var def in DefinitionLoader.LoadTerrains(directory))
        {
            if (!_terrain.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate terrain definition ID '{def.Id}' found.");

            _terrainIndex[def.Id] = index;
            _terrainTiles[def.Id] = BuildTile(def, index);
            index++;
        }
        Console.WriteLine($"[DefinitionRegistry] Loaded {_terrain.Count} terrain definitions.");
    }

    public void LoadTilesets(string directory)
    {
        foreach (var def in DefinitionLoader.LoadTilesets(directory))
        {
            if (!_tilesets.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate tileset definition ID '{def.Id}' found.");
        }
        Console.WriteLine($"[DefinitionRegistry] Loaded {_tilesets.Count} tileset definitions.");
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

    public IReadOnlyCollection<ItemDefinition> Items => _items.Values;
    public IReadOnlyCollection<CreatureDefinition> Creatures => _creatures.Values;

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
