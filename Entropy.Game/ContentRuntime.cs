using Entropy.Content;
using Entropy.Content.Validation;
using Entropy.Engine.Rendering;
using Entropy.Engine.World;
using Entropy.Game.WorldGen;
using OpenTK.Mathematics;

namespace Entropy.Game;

public sealed class ContentRuntime : IDisposable
{
    private readonly string _contentRoot;
    private readonly string _jsonFolder;
    private readonly Shader _shader;
    private readonly Camera _camera;
    private readonly GlyphAtlas _asciiAtlas;
    private readonly List<(string Name, Action Load)> _loadSteps;
    private int _loadStep;

    public DefinitionRegistry Definitions { get; private set; } = new();
    public TilesetDefinition Tileset { get; private set; } = null!;
    public GlyphAtlas TerrainAtlas { get; private set; } = null!;
    public QuadBatcher TerrainBatcher { get; private set; } = null!;

    public IReadOnlyList<string> LoadStepNames => _loadSteps.Select(step => step.Name).ToList();

    public ContentRuntime(string contentRoot, Shader shader, Camera camera, GlyphAtlas asciiAtlas)
    {
        _contentRoot = contentRoot;
        _jsonFolder = Path.Combine(contentRoot, "Json");
        _shader = shader;
        _camera = camera;
        _asciiAtlas = asciiAtlas;

        _loadSteps =
        [
            ("Items", () => Definitions.LoadItems(_jsonFolder)),
            ("Creatures", () => Definitions.LoadCreatures(_jsonFolder)),
            ("Terrain", () => Definitions.LoadTerrains(_jsonFolder)),
            ("Tilesets", () => Definitions.LoadTilesets(_jsonFolder)),
            ("Building templates", () => Definitions.LoadBuildingTemplates(_jsonFolder)),
            ("World objects", () => Definitions.LoadWorldObjects(_jsonFolder)),
            ("Loot tables", () => Definitions.LoadLootTables(_jsonFolder)),
            ("Tileset atlas", FinishInitialLoad)
        ];
    }

    public string? ProcessLoadStep()
    {
        if (_loadStep >= _loadSteps.Count)
            return null;

        var (name, load) = _loadSteps[_loadStep];
        load();
        _loadStep++;
        return name;
    }

    public void Reload(
        MapGraph maps,
        IReadOnlyDictionary<string, BuildingInstance> buildings,
        Action<string, Color4> log)
    {
        try
        {
            var fresh = new DefinitionRegistry();
            fresh.LoadItems(_jsonFolder);
            fresh.LoadCreatures(_jsonFolder);
            fresh.LoadTerrains(_jsonFolder);
            fresh.LoadTilesets(_jsonFolder);
            fresh.LoadBuildingTemplates(_jsonFolder);
            fresh.LoadWorldObjects(_jsonFolder);
            fresh.LoadLootTables(_jsonFolder);

            var errors = DefinitionValidator.Validate(
                fresh.Items, fresh.Creatures, fresh.Terrains,
                fresh.Tilesets, fresh.BuildingTemplates, fresh.WorldObjects, fresh.LootTables);
            if (errors.Count > 0)
            {
                log($"Content reload blocked, {errors.Count} validation error(s):", Color4.Red);
                foreach (var error in errors.Take(3))
                    log("  " + error, Color4.Red);
                return;
            }

            Definitions = fresh;
            Tileset = Definitions.Tileset("entropy_art");

            var replacementAtlas = Tileset.Mode == "art"
                ? new GlyphAtlas(Path.Combine(
                    Path.GetDirectoryName(_contentRoot)!,
                    Tileset.Atlas.Replace('/', Path.DirectorySeparatorChar)))
                : _asciiAtlas;
            TerrainAtlas.Dispose();
            TerrainAtlas = replacementAtlas;
            TerrainBatcher.Dispose();
            TerrainBatcher = new QuadBatcher(_shader, _camera, TerrainAtlas);

            foreach (var map in maps.Maps.Values)
                RestampTerrainTiles(map);

            var restamped = 0;
            foreach (var building in buildings.Values)
            {
                var template = Definitions.BuildingTemplate(building.TemplateId);
                var map = maps[building.MapId];
                if (map.Width != template.Width || map.Height != template.Height)
                {
                    log($"  '{building.Id}' changed size. restart to apply.", Color4.Yellow);
                    continue;
                }

                for (var y = 0; y < template.Height; y++)
                for (var x = 0; x < template.Width; x++)
                {
                    var marker = template.Grid[y][x];
                    map.SetTile(x, y, Definitions.TileOf(template.Legend[marker]));
                }

                restamped++;
            }

            log(
                $"Content reloaded: {Definitions.Terrains.Count} terrains, " +
                $"{Definitions.Items.Count} items, {Definitions.Creatures.Count} creatures, " +
                $"{restamped} building interior(s) restamped.",
                Color4.LightGray);
        }
        catch (Exception ex)
        {
            log($"Content reload failed: {ex.Message}", Color4.Red);
        }
    }

    private void FinishInitialLoad()
    {
        Tileset = Definitions.Tileset("entropy_art");
        TerrainAtlas = Tileset.Mode == "art"
            ? new GlyphAtlas(Path.Combine(
                Path.GetDirectoryName(_contentRoot)!,
                Tileset.Atlas.Replace('/', Path.DirectorySeparatorChar)))
            : _asciiAtlas;
        TerrainBatcher = new QuadBatcher(_shader, _camera, TerrainAtlas);
    }

    private void RestampTerrainTiles(TileMap map)
    {
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
        {
            var tile = map[x, y];
            if (tile.TerrainDefIndex == 0) continue;
            map.SetTile(x, y, Definitions.TileForIndex(tile.TerrainDefIndex));
        }
    }

    public void Dispose()
    {
        TerrainBatcher.Dispose();
        if (!ReferenceEquals(TerrainAtlas, _asciiAtlas))
            TerrainAtlas.Dispose();
    }
}
