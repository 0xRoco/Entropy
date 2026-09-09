using Entropy.Engine.ECS;

namespace Entropy.Game.Components;

public struct Container
{
    public List<Entity> Items;
    public int Slots;

    public static Container Create() => new() { Items = [], Slots = int.MaxValue };

    public static Container WithSlots(int slots) => new() { Items = [], Slots = slots };
}