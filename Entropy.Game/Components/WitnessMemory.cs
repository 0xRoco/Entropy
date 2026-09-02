namespace Entropy.Game.Components;

public struct WitnessMemory
{
    public List<SimEvent> Witnessed;
    public static WitnessMemory Create() => new() { Witnessed = [] };
}