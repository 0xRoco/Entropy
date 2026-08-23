namespace Entropy.Game.Definitions;

public class DefinitionRegistry
{
    private readonly Dictionary<string, ItemDefinition> _items = new();

    public void LoadItems(string directory)
    {
        foreach (var def in DefinitionLoader.LoadItems(directory))
        {
            if (!_items.TryAdd(def.Id, def))
                throw new InvalidOperationException($"Duplicate item definition ID '{def.Id}' found.");
        }
        
        Console.WriteLine($"[DefinitionRegistry] Loaded {_items.Count} item definitions from '{directory}'.");
    }
    
    public ItemDefinition Item(string id) => _items.TryGetValue(id, out var def) 
        ? def
        : throw new KeyNotFoundException($"Item definition with ID '{id}' not found.");
    public IReadOnlyCollection<ItemDefinition> Items => _items.Values;
}