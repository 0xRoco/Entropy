namespace Entropy.Engine.ECS.Components;

/// <summary>
/// Which map an entity is on. Entities are global in one ECS World;
/// spatial queries (rendering, perception, occupancy, pathfinding)
/// filter by this. This is the location fork decided in M5.
/// </summary>
public struct Location
{
    public string MapId;
}
