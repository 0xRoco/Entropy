namespace Entropy.Game.Components.AI;

public struct WitnessMemory
{
    public List<SimEvent> Witnessed;
    public static WitnessMemory Create() => new() { Witnessed = [] };
}
