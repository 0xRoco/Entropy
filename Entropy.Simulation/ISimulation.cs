namespace Entropy.Simulation;

public interface ISimulation
{
    ActionResult Execute(SimulationCommand command);
    int Advance(int minutes);
}
