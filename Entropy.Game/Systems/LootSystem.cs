using Entropy.Content;
using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Tags;

namespace Entropy.Game.Systems;

public static class LootSystem
{
    public static bool Generate(World world, DefinitionRegistry definitions, Rng rng,
        Entity container, LootTableDefinition table)
    {
        if (!world.IsAlive(container) || !world.Has<Container>(container) ||
            world.Has<LootGenerated>(container))
            return false;

        var containerState = world.Get<Container>(container);
        var entries = table.Entries
            .Where(entry => entry.Weight > 0 && entry.MinQuantity > 0 && entry.MaxQuantity >= entry.MinQuantity)
            .ToList();
        var rolls = Math.Max(0, table.Rolls);

        for (var roll = 0; roll < rolls && containerState.Items.Count < containerState.Slots; roll++)
        {
            var entry = Pick(entries, rng);
            if (entry is null) break;

            var quantity = rng.Next(entry.MinQuantity, entry.MaxQuantity + 1);
            var item = EntitySpawner.SpawnIntoContainer(
                world,
                container,
                definitions.Item(entry.ItemId),
                quantity);
            if (!world.IsAlive(item))
                break;

            containerState = world.Get<Container>(container);
        }

        world.Set(container, new LootGenerated());
        return true;
    }

    private static LootEntry? Pick(IReadOnlyList<LootEntry> entries, Rng rng)
    {
        var totalWeight = entries.Sum(entry => entry.Weight);
        if (totalWeight <= 0) return null;

        var roll = rng.Next(totalWeight);
        foreach (var entry in entries)
        {
            roll -= entry.Weight;
            if (roll < 0) return entry;
        }

        return entries[^1];
    }
}
