using Entropy.Engine.ECS;

namespace Entropy.Game.Components;

public struct Container
{
    public List<Entity> Items;
    public static Container Create() => new() { Items = [] };
}