using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Game.Definitions;
using Entropy.Game.Systems;
using Entropy.Game.UI;

namespace Entropy.Game;

public sealed class GameContext
{
    public required TileMap Map { get; init; }
    public required MessageLog Log { get; init; }
    public required World World { get; init; }
    public required DefinitionRegistry Definitions { get; init; }
    public required Rng Rng { get; init; }
    public required Entity Player { get; init; }
    public required WorldClock Clock { get; init; }
    public required TurnProcessor Turns { get; init; }
    public required VisibilityMap Visibility { get; init; }
    public required int ViewRadius { get; init; }
}