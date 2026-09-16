using Entropy.Engine.ECS;

namespace Entropy.Simulation;

public sealed class ActorScheduler
{
    private readonly List<Entity> _actors = [];
    private readonly Dictionary<Entity, int> _energy = [];

    public IReadOnlyList<Entity> Actors => _actors;

    public void Add(Entity entity)
    {
        if (_actors.Contains(entity)) return;
        _actors.Add(entity);
        _energy[entity] = 0;
    }

    public void Remove(Entity entity)
    {
        _actors.Remove(entity);
        _energy.Remove(entity);
    }

    public void RemoveDead(World world)
    {
        foreach (var actor in _actors.Where(actor => !world.IsAlive(actor)).ToList())
            Remove(actor);
    }

    public void AddEnergy(Func<Entity, int> energyOf)
    {
        foreach (var actor in _actors)
            _energy[actor] = _energy.GetValueOrDefault(actor) + energyOf(actor);
    }

    public bool Spend(Entity actor, int actionCost)
    {
        if (_energy.GetValueOrDefault(actor) < actionCost)
            return false;
        _energy[actor] -= actionCost;
        return true;
    }

    public int EnergyOf(Entity actor) => _energy.GetValueOrDefault(actor);

    public void SetEnergy(Entity actor, int energy)
    {
        if (_actors.Contains(actor))
            _energy[actor] = Math.Max(0, energy);
    }

    public void RunReady(
        Entity excludedActor,
        int actionCost,
        Func<Entity, bool> isEligible,
        Func<Entity, ActionResult> execute,
        Func<bool> shouldStop)
    {
        var acted = true;
        while (acted && !shouldStop())
        {
            acted = false;
            foreach (var actor in _actors.Where(isEligible).ToList())
            {
                if (actor.Equals(excludedActor) || !Spend(actor, actionCost))
                    continue;

                _ = execute(actor);
                if (shouldStop())
                    return;
                acted = true;
            }
        }
    }
}
