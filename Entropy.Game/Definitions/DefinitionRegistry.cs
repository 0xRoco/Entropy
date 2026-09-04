namespace Entropy.Game.Definitions;

public class DefinitionRegistry
{
    private readonly Dictionary<string, ItemDefinition> _items = new();
    private readonly Dictionary<string, CreatureDefinition> _creatures = new();

    public void LoadItems(string directory)
    {
        foreach (var def in DefinitionLoader.LoadItems(directory))
        {
            if (!_items.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate item definition ID '{def.Id}' found.");
        }
        
        Console.WriteLine($"[DefinitionRegistry] Loaded {_items.Count} item definitions from '{directory}'.");
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

    public CreatureDefinition Creature(string id) => _creatures.TryGetValue(id, out var def)
        ? def
        : throw new KeyNotFoundException($"Creature definition with ID '{id}' not found.");
    
    public ItemDefinition Item(string id) => _items.TryGetValue(id, out var def) 
        ? def
        : throw new KeyNotFoundException($"Item definition with ID '{id}' not found.");
    public IReadOnlyCollection<ItemDefinition> Items => _items.Values;
    public IReadOnlyCollection<CreatureDefinition> Creatures => _creatures.Values;
}