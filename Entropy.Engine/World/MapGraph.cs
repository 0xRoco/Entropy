using OpenTK.Mathematics;

namespace Entropy.Engine.World;

public record MapTransition(
    string FromMap,
    Vector2i FromTile,
    string ToMap,
    Vector2i ToTile);

public class MapGraph
{
    private readonly Dictionary<string, TileMap> _maps = new();
    private readonly List<MapTransition> _transitions = new();

    private readonly Dictionary<(string Map, int X, int Y), MapTransition> _byTile = new();

    public TileMap this[string mapId] => _maps.TryGetValue(mapId, out var map)
        ? map
        : throw new KeyNotFoundException($"Map '{mapId}' not found in graph.");

    public IReadOnlyCollection<TileMap> Maps => _maps.Values;
    public IReadOnlyList<MapTransition> Transitions => _transitions;

    public void AddMap(string mapId, TileMap map)
    {
        if (!_maps.TryAdd(mapId, map))
            throw new InvalidOperationException($"Duplicate map id '{mapId}'.");
    }

    public void Connect(string fromMap, Vector2i fromTile, string toMap, Vector2i toTile)
    {
        var forward = new MapTransition(fromMap, fromTile, toMap, toTile);
        var backward = new MapTransition(toMap, toTile, fromMap, fromTile);
        _transitions.Add(forward);
        _transitions.Add(backward);
        _byTile[(fromMap, fromTile.X, fromTile.Y)] = forward;
        _byTile[(toMap, toTile.X, toTile.Y)] = backward;
    }

    public MapTransition? TransitionAt(string mapId, Vector2i tile) =>
        _byTile.GetValueOrDefault((mapId, tile.X, tile.Y));
}
