using Entropy.Engine.ECS;

namespace Entropy.Game.Systems;

public sealed class SimulationRuntime : ISimulation
{
    private readonly Entropy.Simulation.SimulationState _state;
    private readonly MinuteProcessor _minuteProcessor;

    public SimulationRuntime(Entropy.Simulation.SimulationState state, IGameRuntimeContext runtime)
    {
        _state = state;
        Runtime = runtime;
        _minuteProcessor = new MinuteProcessor(runtime, new TurnProcessor(_state.Scheduler));
    }

    private IGameRuntimeContext Runtime { get; }
    public World World => _state.World;
    public Entity Player => _state.Player;

    public int Advance(int minutes)
    {
        var advanced = _state.Advance(minutes, _minuteProcessor.Process);
        return advanced;
    }

    public ActionResult Execute(SimulationCommand command)
    {
        var result = PlayerActions.Execute(command, Runtime, _state);
        return result;
    }
}
