using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Entropy.Engine.ECS;

public class World
{
    private int _nextId = 1;
    private long _nextStableId = 1;
    private readonly Dictionary<Type, object> _storages = new();
    private readonly HashSet<int> _entities = [];
    private readonly Dictionary<int, long> _stableIds = [];
    private readonly Dictionary<long, int> _entitiesByStableId = [];

    public Entity Create()
    {
        var id = _nextId++;
        _entities.Add(id);
        var stableId = _nextStableId++;
        _stableIds[id] = stableId;
        _entitiesByStableId[stableId] = id;
        return new Entity(id);
    }

    public Entity Create(long stableId)
    {
        if (stableId <= 0 || _entitiesByStableId.ContainsKey(stableId))
            throw new ArgumentOutOfRangeException(nameof(stableId));

        var entity = Create();
        _entitiesByStableId.Remove(_stableIds[entity.Id]);
        _stableIds[entity.Id] = stableId;
        _entitiesByStableId[stableId] = entity.Id;
        _nextStableId = Math.Max(_nextStableId, stableId + 1);
        return entity;
    }

    public Entity Create(long requestedStableId, out long assignedStableId)
    {
        if (requestedStableId > 0 && !_entitiesByStableId.ContainsKey(requestedStableId))
        {
            var entity = Create(requestedStableId);
            assignedStableId = requestedStableId;
            return entity;
        }

        var remapped = Create();
        assignedStableId = StableId(remapped);
        return remapped;
    }

    public void Destroy(Entity entity)
    {
        if (!_entities.Remove(entity.Id)) return;
        if (_stableIds.Remove(entity.Id, out var stableId))
            _entitiesByStableId.Remove(stableId);
        foreach (var storage in _storages.Values)
        {
            ((IComponentStorage)storage).Remove(entity.Id);
        }
    }

    public bool IsAlive(Entity entity) => _entities.Contains(entity.Id);

    public long StableId(Entity entity)
    {
        EnsureAlive(entity);
        return _stableIds[entity.Id];
    }

    public Entity ResolveStableId(long stableId) =>
        _entitiesByStableId.TryGetValue(stableId, out var id)
            ? new Entity(id)
            : default;
    
    public void Set<T>(Entity entity, T component)
    {
        EnsureAlive(entity);
        GetStorage<T>()[entity.Id] = component;
    }
    
    public ref T Get<T>(Entity entity)
    {
        EnsureAlive(entity);
        ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(GetStorage<T>(), entity.Id);
        if (Unsafe.IsNullRef(ref value))
            throw new InvalidOperationException($"Entity {entity.Id} does not have component of type {typeof(T)}");
        return ref value!;
    }
    
    public bool Has<T>(Entity entity)
    {
        EnsureAlive(entity);
        return GetStorage<T>().ContainsKey(entity.Id);
    }

    public void Remove<T>(Entity entity)
    {
        EnsureAlive(entity);
        GetStorage<T>().Remove(entity.Id);
    }

    private void EnsureAlive(Entity entity)
    {
        if (!IsAlive(entity))
            throw new InvalidOperationException($"Entity {entity.Id} is not alive.");
    }
    
    public IEnumerable<Entity> Query<T1>()
    {
        foreach (var id in GetStorage<T1>().Keys)
            yield return new Entity(id);    }
    
    public IEnumerable<Entity> Query<T1, T2>()
    {
        var s1 = GetStorage<T1>();
        var s2 = GetStorage<T2>();
        if (s1.Count < s2.Count)
        {
            foreach (var id in s1.Keys.Where(s2.ContainsKey))
            {
                yield return new Entity(id);
            }
        }else
        {
            foreach (var id in s2.Keys.Where(s1.ContainsKey))
            {
                yield return new Entity(id);
            }
        }
    }
    
    private Dictionary<int, T> GetStorage<T>()
    {
        var type = typeof(T);
        if (_storages.TryGetValue(type, out var storage)) return (Dictionary<int, T>)storage;
        
        storage = new ComponentStorage<T>();
        _storages[type] = storage;
        return (Dictionary<int, T>)storage;
    }
    
    private interface IComponentStorage
    {
        void Remove(int id);
    }
    private sealed class ComponentStorage<T> : Dictionary<int,T>, IComponentStorage
    {
        public new void Remove(int id) => base.Remove(id);
    }
}
