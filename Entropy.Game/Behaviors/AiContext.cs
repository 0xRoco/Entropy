using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;

namespace Entropy.Game.Behaviors;

public sealed class AiContext
{
    public required World World { get; init; }
    public required Entity Player { get; init; }
    public required MapGraph Maps { get; init; }
    public required Rng Rng { get; init; }
    public required WorldClock Clock { get; init; }
    public required string MapId { get; init; }
    public required TileMap Map { get; init; }
    public required VisibilityMap Visibility { get; init; }

}
