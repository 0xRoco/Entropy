using System.Text.Json;
using Entropy.Engine.World;

namespace Entropy.Game.WorldGen;

public sealed record AuthoredMap(MapDefinition Definition, TileMap Map);

public static class AuthoredMapLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AuthoredMap Load(string path, DefinitionRegistry definitions)
    {
        var definition = JsonSerializer.Deserialize<MapDefinition>(File.ReadAllText(path), Options)
                         ?? throw new InvalidOperationException($"Map file '{path}' is empty.");
        var errors = MapDefinitionValidator.Validate(definition);
        if (errors.Count > 0)
            throw new InvalidOperationException($"Map '{definition.Id}' is invalid: {string.Join("; ", errors)}");

        var map = new TileMap(definition.Width, definition.Height);
        for (var y = 0; y < definition.Height; y++)
        for (var x = 0; x < definition.Width; x++)
        {
            var symbol = definition.Terrain.Rows[y][x].ToString();
            map.SetTile(x, y, definitions.TileOf(definition.Terrain.Legend[symbol]));
        }

        return new AuthoredMap(definition, map);
    }
}
