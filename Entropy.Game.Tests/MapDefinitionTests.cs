using Entropy.Engine.World;
using Entropy.Engine.Core;
using Entropy.Game.WorldGen;
using Entropy.Simulation;
using Xunit;

namespace Entropy.Game.Tests;

public sealed class MapDefinitionTests
{
    [Fact]
    public void AuthoredMapLoadsTerrainFromJson()
    {
        var path = WriteMap("""
            {
              "version": 1,
              "id": "test_map",
              "name": "Test map",
              "width": 4,
              "height": 2,
              "terrain": {
                "legend": { ".": "floor_linoleum", "#": "wall_concrete" },
                "rows": ["....", ".##."]
              },
              "anchors": [{ "id": "entry", "kind": "player_spawn", "x": 0, "y": 0 }]
            }
            """);

        try
        {
            var definitions = new DefinitionRegistry();
            definitions.LoadTerrains(ContentPath());
            var authored = AuthoredMapLoader.Load(path, definitions);

            Assert.Equal("test_map", authored.Definition.Id);
            Assert.Equal(4, authored.Map.Width);
            Assert.Equal(2, authored.Map.Height);
            Assert.True(authored.Map[0, 0].Walkable);
            Assert.False(authored.Map[1, 1].Walkable);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidatorRejectsInvalidRowsAndCoordinates()
    {
        var definition = new MapDefinition
        {
            Id = "invalid",
            Width = 2,
            Height = 1,
            Terrain = new MapTerrainLayer
            {
                Legend = new Dictionary<string, string> { ["."] = "floor_linoleum" },
                Rows = ["..."]
            },
            Anchors = [new MapAnchor { Id = "entry", Kind = "spawn", X = 3, Y = 0 }]
        };

        var errors = MapDefinitionValidator.Validate(definition);

        Assert.Contains(errors, error => error.Contains("terrain row 0", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("anchor 'entry'", StringComparison.Ordinal));
    }

    [Fact]
    public void ProceduralOutsideUsesSeedAndStaysWithinBounds()
    {
        var definitions = new DefinitionRegistry();
        definitions.LoadTerrains(ContentPath());
        definitions.LoadBuildingTemplates(ContentPath());

        var first = ProceduralOutsideGenerator.Generate(definitions, new Rng(42), 48, 30);
        var second = ProceduralOutsideGenerator.Generate(definitions, new Rng(42), 48, 30);

        Assert.Equal(first.Map.Width, second.Map.Width);
        Assert.Equal(first.Map.Height, second.Map.Height);
        Assert.Equal(
            first.Buildings.Select(building => $"{building.TemplateId}:{building.Origin.X}:{building.Origin.Y}"),
            second.Buildings.Select(building => $"{building.TemplateId}:{building.Origin.X}:{building.Origin.Y}"));
        Assert.All(first.Buildings, building =>
        {
            var template = definitions.BuildingTemplate(building.TemplateId);
            Assert.InRange(building.Origin.X, 0, first.Map.Width - template.Width);
            Assert.InRange(building.Origin.Y, 0, first.Map.Height - template.Height);
        });
    }

    private static string WriteMap(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"entropy-map-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, contents);
        return path;
    }

    private static string ContentPath() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
}
