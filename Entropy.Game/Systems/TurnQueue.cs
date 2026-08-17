using Entropy.Engine.ECS;

namespace Entropy.Game.Systems;

public class TurnQueue
{
    private readonly List<Entity> _actors = [];
    private int _currentIndex;

    public void Add(Entity entity) => _actors.Add(entity);

    public void Remove(Entity entity)
    {
        var index = _actors.IndexOf(entity);
        if (index == -1) return;
        _actors.RemoveAt(index);
        if (index < _currentIndex) _currentIndex--;
        if (_currentIndex >= _actors.Count && _actors.Count > 0) _currentIndex = 0;
    }

    public Entity GetCurrent => _actors[_currentIndex];
    public void Advance()
    {
        _currentIndex = (_currentIndex + 1) % _actors.Count;
        //Console.WriteLine($"Turn advanced to entity {_actors[_currentIndex].Id}. Current index: {_currentIndex}");
    }

    public bool IsEmpty => _actors.Count == 0;
}