namespace Entropy.Content;

public sealed class LootTableDefinition
{
    public required string Id { get; set; }
    public int Rolls { get; set; } = 1;
    public List<LootEntry> Entries { get; set; } = [];
}

public sealed class LootEntry
{
    public required string ItemId { get; set; }
    public int Weight { get; set; } = 1;
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
}
