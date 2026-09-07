using Entropy.Engine.World;
using OpenTK.Mathematics;

namespace Entropy.Game.WorldGen;

public record BuildingInstance(
    string Id,
    string TemplateId,
    string MapId,
    Vector2i ExteriorDoor,
    IReadOnlyDictionary<string, Vector2i> Anchors);

public record CityBlock(
    MapGraph Maps,
    string StreetMapId,
    IReadOnlyDictionary<string, BuildingInstance> Buildings);