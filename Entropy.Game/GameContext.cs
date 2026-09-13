using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Content;
using Entropy.Game.Systems;
using Entropy.Game.UI;

namespace Entropy.Game;

public sealed class GameContext
{
    public required TileMap Map { get; set; }
    public required string MapId { get; set; }
    public required MapGraph Maps { get; init; }
    public required MessageLog Log { get; init; }
    public required World World { get; init; }
    public required DefinitionRegistry Definitions { get; init; }
    public required Rng Rng { get; init; }
    public required Entity Player { get; init; }
    public required WorldClock Clock { get; init; }
    public required TurnProcessor Turns { get; init; }
    public required Dictionary<string, VisibilityMap> Visibilities { get; init; }
    public required VisibilityMap Visibility { get; set; }
    public required int ViewRadius { get; init; }
    public required DemoObjective Objective { get; init; }
    public required HashSet<string> LockedMaps { get; init; }
}
