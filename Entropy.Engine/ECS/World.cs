using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Entropy.Engine.ECS;

public class World
{
    private int _nextId = 1;
    private readonly Dictionary<Type, object> _storages = new();
    private readonly HashSet<int> _entities = [];

    public Entity Create()
    {
        var id = _nextId++;
        _entities.Add(id);
        return new Entity(id);
    }

    public void Destroy(Entity entity)
    {
        if (!_entities.Remove(entity.Id)) return;
        foreach (var storage in _storages.Values)
        {
            ((IComponentStorage)storage).Remove(entity.Id);
            
            Console.WriteLine($"Removed entity {entity.Id} from storage of type {storage.GetType().GetGenericArguments()[0].Name}");
        }
    }

    public bool IsAlive(Entity entity) => _entities.Contains(entity.Id);
    
    public void Set<T>(Entity entity, T component)
    {
        GetStorage<T>()[entity.Id] = component;
    }
    
    public ref T Get<T>(Entity entity)
    {
        ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(GetStorage<T>(), entity.Id);
        if (Unsafe.IsNullRef(ref value))
            throw new InvalidOperationException($"Entity {entity.Id} does not have component of type {typeof(T)}");
        return ref value;
    }
    
    public bool Has<T>(Entity entity) => GetStorage<T>().ContainsKey(entity.Id);
    
    public void Remove<T>(Entity entity) => GetStorage<T>().Remove(entity.Id);
    
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