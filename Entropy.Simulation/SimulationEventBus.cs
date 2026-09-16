namespace Entropy.Simulation;

public sealed class SimulationEventBus
{
    private readonly Queue<SimEvent> _pending = new();
    private readonly List<SimEvent> _history = [];

    public IReadOnlyList<SimEvent> History => _history.AsReadOnly();

    public void Publish(SimEvent simulationEvent)
    {
        _pending.Enqueue(simulationEvent);
        _history.Add(simulationEvent);
    }

    public IReadOnlyList<SimEvent> Drain()
    {
        var events = new List<SimEvent>(_pending.Count);
        while (_pending.TryDequeue(out var simulationEvent))
            events.Add(simulationEvent);
        return events;
    }
}
