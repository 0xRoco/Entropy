using Entropy.Content;
using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Tags;
using Entropy.Game.Systems;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class LootSystemTests
{
    [Fact]
    public void SameSeedProducesSameLoot()
    {
        var first = CreateFixture(1234);
        var second = CreateFixture(1234);
        var table = CreateTable();

        LootSystem.Generate(first.World, first.Definitions, first.Rng, first.Container, table);
        LootSystem.Generate(second.World, second.Definitions, second.Rng, second.Container, table);

        Assert.Equal(ItemSummary(first), ItemSummary(second));
    }

    [Fact]
    public void LootIsGeneratedOnlyOnce()
    {
        var fixture = CreateFixture(1234);
        var table = CreateTable();

        Assert.True(LootSystem.Generate(
            fixture.World, fixture.Definitions, fixture.Rng, fixture.Container, table));
        var firstContents = ItemSummary(fixture);

        Assert.False(LootSystem.Generate(
            fixture.World, fixture.Definitions, fixture.Rng, fixture.Container, table));

        Assert.Equal(firstContents, ItemSummary(fixture));
        Assert.True(fixture.World.Has<LootGenerated>(fixture.Container));
    }

    [Fact]
    public void GenerationRespectsContainerCapacity()
    {
        var fixture = CreateFixture(1234, slots: 1);
        var table = CreateTable(rolls: 5);

        Assert.True(LootSystem.Generate(
            fixture.World, fixture.Definitions, fixture.Rng, fixture.Container, table));

        Assert.Single(fixture.World.Get<Container>(fixture.Container).Items);
    }

    [Fact]
    public void InvalidTableEntriesAreRejectedByValidation()
    {
        var errors = Content.Validation.DefinitionValidator.ValidateLootTables(
            [
                new LootTableDefinition
                {
                    Id = "invalid",
                    Entries =
                    [
                        new LootEntry
                        {
                            ItemId = "missing",
                            Weight = 0,
                            MinQuantity = 2,
                            MaxQuantity = 1
                        }
                    ]
                }
            ],
            ["known"]);

        Assert.Contains(errors, error => error.Contains("unknown item", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("non-positive weight", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("invalid quantity", StringComparison.Ordinal));
    }

    private static LootTableDefinition CreateTable(int rolls = 2) => new()
    {
        Id = "test_food",
        Rolls = rolls,
        Entries =
        [
            new LootEntry { ItemId = "crackers", Weight = 2, MinQuantity = 1, MaxQuantity = 2 },
            new LootEntry { ItemId = "canned_beans", Weight = 1, MinQuantity = 1, MaxQuantity = 1 }
        ]
    };

    private static string[] ItemSummary(Fixture fixture) =>
        fixture.World.Get<Container>(fixture.Container).Items
            .Select(item =>
            {
                var identity = fixture.World.Get<ItemIdentity>(item);
                var count = fixture.World.Has<Stackable>(item)
                    ? fixture.World.Get<Stackable>(item).Count
                    : 1;
                return $"{identity.DefinitionId}:{count}";
            })
            .ToArray();

    private static Fixture CreateFixture(int seed, int slots = 6)
    {
        var world = new World();
        var container = world.Create();
        world.Set(container, new Position { Value = new Vector2(1, 1) });
        world.Set(container, new Location { MapId = "test" });
        world.Set(container, Container.WithSlots(slots));

        var definitions = new DefinitionRegistry();
        definitions.LoadItems(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json")));

        return new Fixture(world, container, definitions, new Rng(seed));
    }

    private sealed record Fixture(
        World World,
        Entity Container,
        DefinitionRegistry Definitions,
        Rng Rng);
}