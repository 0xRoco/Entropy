using System.Text.Json;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.AI;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.ItemEffects;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using Entropy.Simulation;

namespace Entropy.Game.Systems;

public sealed record GameSaveData(
    int Version,
    int Seed,
    int ElapsedMinutes,
    string MapId,
    int PlayerX,
    int PlayerY,
    int FacingX,
    int FacingY,
    HealthData Health,
    HungerData Hunger,
    ThirstData Thirst,
    FatigueData Fatigue,
    IReadOnlyList<InventoryItemData> Inventory,
    string? EquippedItemId,
    CharacterData? Character,
    int CashCents = 0,
    IReadOnlyList<WorldEntityData>? WorldEntities = null,
    IReadOnlyList<DoorStateData>? DoorStates = null,
    IReadOnlyList<ContainerStateData>? Containers = null,
    IReadOnlyList<WorldItemData>? WorldItems = null,
    ActivityData? PlayerActivity = null,
    long? EquippedItemStableId = null,
    ArrivalStateData? Arrival = null);

public sealed record HealthData(int Current, int Max);
public sealed record HungerData(int Current, int Max, bool Starving);
public sealed record ThirstData(int Current, int Max, bool Parched);
public sealed record FatigueData(int Current, int Max);
public sealed record InventoryItemData(string DefinitionId, int Count, long? StableId = null);
public sealed record CharacterData(
    string Name,
    string ProfessionId,
    string BackgroundId,
    Dictionary<string, int> Stats,
    IReadOnlyList<string> TraitIds,
    IReadOnlyList<string> SkillIds);
public sealed record WorldEntityData(
    long StableId,
    string MapId,
    int X,
    int Y,
    int FacingX,
    int FacingY,
    string? Name,
    HealthData? Health,
    string? DefinitionId = null,
    bool Wanted = false,
    bool Hostile = false,
    bool Searched = false,
    bool LootGenerated = false,
    int SchedulerEnergy = 0,
    AiStateData? AiState = null,
    IReadOnlyList<AwarenessData>? Awareness = null,
    string? SaveId = null,
    string? SpawnKind = null,
    ActivityData? Activity = null,
    IReadOnlyList<WitnessEventData>? Witnessed = null);
public sealed record DoorStateData(
    string FromMap,
    int FromX,
    int FromY,
    string ToMap,
    int ToX,
    int ToY,
    bool Locked,
    bool Broken,
    bool TrespassReported);
public sealed record ContainerStateData(
    long ContainerStableId,
    IReadOnlyList<long> ItemStableIds,
    IReadOnlyList<InventoryItemData>? Items = null,
    string? DefinitionId = null,
    string? SaveId = null,
    bool Locked = false,
    bool Broken = false,
    bool TrespassReported = false);
public sealed record AiStateData(
    AIMode Mode,
    long? TargetStableId,
    int? DestinationX,
    int? DestinationY,
    int? LastKnownX,
    int? LastKnownY,
    int AlertLevel,
    int TurnsSinceContact);
public sealed record AwarenessData(
    long TargetStableId,
    bool Detected,
    int? LastKnownX,
    int? LastKnownY,
    int TurnsSinceDetected);
public sealed record WorldItemData(
    long StableId,
    string DefinitionId,
    int Count,
    string MapId,
    int X,
    int Y);
public sealed record ActivityData(
    ActivityKind Kind,
    int RemainingMinutes,
    int TotalMinutes,
    long? TargetStableId);
public sealed record WitnessEventData(
    string Type,
    string Description,
    string MapId,
    int X,
    int Y,
    int Minute,
    long? ActorStableId,
    long? TargetStableId);
public sealed record ArrivalStateData(
    string LegalIdentityStatus,
    bool HasTemporaryPermit,
    int PermitExpiryMinute,
    bool EntryRestricted,
    bool HasEnteredSpire);

public static class GameSave
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private sealed class StableIdRemap
    {
        private readonly Dictionary<long, long> _ids = [];

        public void Add(long savedId, Entity entity, World world) => _ids[savedId] = world.StableId(entity);
        public long? Resolve(long? savedId) => savedId is { } id && _ids.TryGetValue(id, out var remapped) ? remapped : savedId;
    }

    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Entropy",
        "save.json");

    public static bool Exists => File.Exists(Path);

    public static void Write(GameSaveData data)
    {
        var directory = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, Options));
            File.Move(temporaryPath, Path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public static GameSaveData? Read() => Read(Path);

    public static GameSaveData? Read(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var data = JsonSerializer.Deserialize<GameSaveData>(File.ReadAllText(path));
            if (data is null || data.Health is null || data.Hunger is null ||
                data.Thirst is null || data.Fatigue is null || data.Inventory is null ||
                string.IsNullOrWhiteSpace(data.MapId))
                return null;

            return data is { Version: CurrentVersion } ? data : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static GameSaveData Capture(
        World world,
        Entity player,
        int seed,
        int elapsedMinutes,
        string mapId,
        CharacterData? character = null,
        ActorScheduler? scheduler = null)
    {
        var position = world.Get<Entropy.Engine.ECS.Components.Position>(player).Value;
        var facing = world.Has<Facing>(player)
            ? world.Get<Facing>(player).Direction
            : new OpenTK.Mathematics.Vector2i(1, 0);
        var health = world.Get<Health>(player);
        var hunger = world.Get<Hunger>(player);
        var thirst = world.Get<Thirst>(player);
        var fatigue = world.Get<Fatigue>(player);
        var cashCents = world.Has<Wallet>(player) ? world.Get<Wallet>(player).CashCents : 0;
        var inventory = world.Has<Container>(player)
            ? world.Get<Container>(player).Items
                .Where(world.IsAlive)
                .Where(item => world.Has<ItemIdentity>(item))
                .Select(item => new InventoryItemData(
                    world.Get<ItemIdentity>(item).DefinitionId,
                    world.Has<Stackable>(item) ? world.Get<Stackable>(item).Count : 1,
                    world.StableId(item)))
                .ToList()
            : [];

        var worldEntities = world.Query<Location, Position>()
            .Where(entity => !entity.Equals(player) && !world.Has<ItemIdentity>(entity))
            .Select(entity => new WorldEntityData(
                world.StableId(entity),
                world.Get<Location>(entity).MapId,
                (int)world.Get<Position>(entity).Value.X,
                (int)world.Get<Position>(entity).Value.Y,
                world.Has<Facing>(entity) ? world.Get<Facing>(entity).Direction.X : 1,
                world.Has<Facing>(entity) ? world.Get<Facing>(entity).Direction.Y : 0,
                world.Has<Named>(entity) ? world.Get<Named>(entity).Name : null,
                world.Has<Health>(entity)
                    ? new HealthData(world.Get<Health>(entity).Current, world.Get<Health>(entity).Max)
                    : null,
                world.Has<CreatureIdentity>(entity)
                    ? world.Get<CreatureIdentity>(entity).DefinitionId
                    : world.Has<WorldObjectIdentity>(entity)
                        ? world.Get<WorldObjectIdentity>(entity).DefinitionId
                        : null,
                world.Has<Wanted>(entity),
                world.Has<Hostile>(entity),
                world.Has<Searched>(entity),
                world.Has<LootGenerated>(entity),
                scheduler?.EnergyOf(entity) ?? 0,
                CaptureAiState(world, entity),
                CaptureAwareness(world, entity),
                world.Has<PersistentIdentity>(entity)
                    ? world.Get<PersistentIdentity>(entity).Id
                    : null,
                world.Has<CreatureIdentity>(entity)
                    ? "creature"
                    : world.Has<WorldObjectIdentity>(entity)
                        ? "object"
                        : null,
                 CaptureActivity(world, entity),
                 CaptureWitnessed(world, entity)))
            .ToList();

        var equippedItem = world.Has<Equipped>(player) &&
                             world.IsAlive(world.Get<Equipped>(player).Item.Resolve(world)) &&
                             world.Has<ItemIdentity>(world.Get<Equipped>(player).Item.Resolve(world))
            ? world.Get<Equipped>(player).Item.Resolve(world)
            : (Entity?)null;
        var equippedItemId = equippedItem is { } equipped
            ? world.Get<ItemIdentity>(equipped).DefinitionId
            : null;
        var equippedItemStableId = equippedItem is { } equippedEntity
            ? world.StableId(equippedEntity)
            : (long?)null;

        return new GameSaveData(
            CurrentVersion,
            seed,
            elapsedMinutes,
            mapId,
            (int)position.X,
            (int)position.Y,
            facing.X,
            facing.Y,
            new HealthData(health.Current, health.Max),
            new HungerData(hunger.Current, hunger.Max, hunger.Starving),
            new ThirstData(thirst.Current, thirst.Max, thirst.Parched),
            new FatigueData(fatigue.Current, fatigue.Max),
            inventory,
            equippedItemId,
            character,
            cashCents,
            worldEntities) with { EquippedItemStableId = equippedItemStableId };
    }

    public static GameSaveData Capture(GameContext context) =>
        Capture(
            context.World,
            context.Player,
            context.Rng.Seed,
            context.Clock.TotalMinutes,
            context.MapId,
            context.World.Has<CharacterIdentity>(context.Player)
                ? ToCharacterData(context.World.Get<CharacterIdentity>(context.Player))
                : null,
            context.Scheduler) with
        {
            DoorStates = context.DoorStates.Select(pair => new DoorStateData(
                pair.Key.FromMap,
                pair.Key.FromTile.X,
                pair.Key.FromTile.Y,
                pair.Key.ToMap,
                pair.Key.ToTile.X,
                pair.Key.ToTile.Y,
                pair.Value.Locked,
                pair.Value.Broken,
                pair.Value.TrespassReported)).ToList(),
            Containers = context.World.Query<Container>()
                .Where(container => !container.Equals(context.Player))
                .Select(container => new ContainerStateData(
                    context.World.StableId(container),
                    context.World.Get<Container>(container).Items
                        .Where(context.World.IsAlive)
                        .Select(context.World.StableId)
                        .ToList(),
                    context.World.Get<Container>(container).Items
                        .Where(context.World.IsAlive)
                        .Where(item => context.World.Has<ItemIdentity>(item))
                .Select(item => new InventoryItemData(
                    context.World.Get<ItemIdentity>(item).DefinitionId,
                    context.World.Has<Stackable>(item) ? context.World.Get<Stackable>(item).Count : 1,
                    context.World.StableId(item)))
                        .ToList(),
                    context.World.Has<WorldObjectIdentity>(container)
                        ? context.World.Get<WorldObjectIdentity>(container).DefinitionId
                        : null,
                    context.World.Has<PersistentIdentity>(container)
                        ? context.World.Get<PersistentIdentity>(container).Id
                        : null,
                    context.World.Has<LockState>(container) && context.World.Get<LockState>(container).Locked,
                    context.World.Has<LockState>(container) && context.World.Get<LockState>(container).Broken,
                    false))
                .ToList(),
            WorldItems = context.World.Query<ItemIdentity>()
                .Where(item => context.World.Has<Location>(item) && !context.World.Has<InContainer>(item))
                .Select(item => new WorldItemData(
                    context.World.StableId(item),
                    context.World.Get<ItemIdentity>(item).DefinitionId,
                    context.World.Has<Stackable>(item) ? context.World.Get<Stackable>(item).Count : 1,
                    context.World.Get<Location>(item).MapId,
                    context.World.Has<Position>(item) ? (int)context.World.Get<Position>(item).Value.X : 0,
                    context.World.Has<Position>(item) ? (int)context.World.Get<Position>(item).Value.Y : 0))
                .ToList(),
            PlayerActivity = CaptureActivity(context.World, context.Player),
            Arrival = new ArrivalStateData(
                context.Arrival.LegalIdentityStatus,
                context.Arrival.HasTemporaryPermit,
                context.Arrival.PermitExpiryMinute,
                context.Arrival.EntryRestricted,
                context.Arrival.HasEnteredSpire)
        };

    public static void RestorePlayer(GameContext context, GameSaveData save)
    {
        var stableIdRemap = new StableIdRemap();
        context.Clock.Advance(save.ElapsedMinutes);

        ref var position = ref context.World.Get<Position>(context.Player);
        position.Value = new OpenTK.Mathematics.Vector2(save.PlayerX, save.PlayerY);
        context.World.Set(context.Player, new Location { MapId = save.MapId });
        context.World.Set(context.Player, new Facing { Direction = new OpenTK.Mathematics.Vector2i(save.FacingX, save.FacingY) });
        context.World.Set(context.Player, new Health { Current = save.Health.Current, Max = save.Health.Max });
        context.World.Set(context.Player, new Hunger
        {
            Current = save.Hunger.Current,
            Max = save.Hunger.Max,
            Starving = save.Hunger.Starving
        });
        context.World.Set(context.Player, new Thirst
        {
            Current = save.Thirst.Current,
            Max = save.Thirst.Max,
            Parched = save.Thirst.Parched
        });
        context.World.Set(context.Player, new Fatigue { Current = save.Fatigue.Current, Max = save.Fatigue.Max });
        context.World.Set(context.Player, new Wallet { CashCents = save.CashCents });

        if (save.Arrival is { } arrival)
        {
            context.Arrival.LegalIdentityStatus = arrival.LegalIdentityStatus;
            context.Arrival.HasTemporaryPermit = arrival.HasTemporaryPermit;
            context.Arrival.PermitExpiryMinute = arrival.PermitExpiryMinute;
            context.Arrival.EntryRestricted = arrival.EntryRestricted;
            context.Arrival.HasEnteredSpire = arrival.HasEnteredSpire;
        }

        if (save.Character is { } character)
        {
            context.World.Set(context.Player, new CharacterIdentity
            {
                Name = character.Name,
                ProfessionId = character.ProfessionId,
                BackgroundId = character.BackgroundId,
                Stats = new(character.Stats, StringComparer.OrdinalIgnoreCase),
                TraitIds = [.. character.TraitIds],
                SkillIds = [.. character.SkillIds]
            });
            context.World.Set(context.Player, new Named { Name = character.Name });
        }

        if (context.World.Has<Container>(context.Player))
        {
            ref var inventoryState = ref context.World.Get<Container>(context.Player);
            foreach (var item in inventoryState.Items.Where(context.World.IsAlive).ToList())
                context.World.Destroy(item);
            inventoryState.Items = [];
        }

        if (context.World.Has<Equipped>(context.Player))
            context.World.Remove<Equipped>(context.Player);

        foreach (var item in save.Inventory)
        {
            var entity = EntitySpawner.CreateItem(
                context.World,
                save.MapId,
                context.Definitions.Item(item.DefinitionId),
                save.PlayerX,
                save.PlayerY,
                item.Count,
                item.StableId);
            ItemSystem.Transfer(context.World, entity, context.Player);
            if (item.StableId is { } savedId)
                stableIdRemap.Add(savedId, entity, context.World);

            var isEquipped = save.EquippedItemStableId is { } equippedStableId
                ? stableIdRemap.Resolve(equippedStableId) == context.World.StableId(entity)
                : save.EquippedItemId == item.DefinitionId;
            if (isEquipped && context.World.Has<Damage>(entity))
                context.World.Set(context.Player, new Equipped { Item = StableEntityReference.From(context.World, entity) });
        }

        if (save.WorldEntities is { } savedEntities)
        {
            var savedStableIds = savedEntities
                .Select(savedEntity => savedEntity.StableId)
                .ToHashSet();
            var savedPersistentIds = savedEntities
                .Where(savedEntity => savedEntity.SaveId is not null)
                .Select(savedEntity => savedEntity.SaveId!)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var entity in context.World.Query<Location, Position>()
                         .Where(entity => !entity.Equals(context.Player) && !context.World.Has<ItemIdentity>(entity))
                         .Where(entity => !savedStableIds.Contains(context.World.StableId(entity)) &&
                                          (!context.World.Has<PersistentIdentity>(entity) ||
                                           !savedPersistentIds.Contains(context.World.Get<PersistentIdentity>(entity).Id)))
                         .ToList())
                context.World.Destroy(entity);

            context.Scheduler.RemoveDead(context.World);

            foreach (var savedEntity in savedEntities)
            {
                var entity = ResolveWorldEntity(context, savedEntity);
                if (context.World.IsAlive(entity))
                    stableIdRemap.Add(savedEntity.StableId, entity, context.World);
            }
        }

        foreach (var savedEntity in save.WorldEntities ?? [])
        {
            if (!context.Maps.Maps.ContainsKey(savedEntity.MapId))
                continue;
            var entity = ResolveWorldEntity(context, savedEntity);
            if (!context.World.IsAlive(entity))
                continue;

            if (context.World.Has<Position>(entity))
                context.World.Get<Position>(entity).Value = new OpenTK.Mathematics.Vector2(savedEntity.X, savedEntity.Y);
            else
                context.World.Set(entity, new Position { Value = new OpenTK.Mathematics.Vector2(savedEntity.X, savedEntity.Y) });

            context.World.Set(entity, new Location { MapId = savedEntity.MapId });
            context.World.Set(entity, new Facing
            {
                Direction = new OpenTK.Mathematics.Vector2i(savedEntity.FacingX, savedEntity.FacingY)
            });
            if (savedEntity.Name is not null)
                context.World.Set(entity, new Named { Name = savedEntity.Name });
            if (savedEntity.Health is { } health)
                context.World.Set(entity, new Health { Current = health.Current, Max = health.Max });
            if (savedEntity.AiState is { } savedAi)
            {
                var aiState = new AIState
                {
                    Mode = savedAi.Mode,
                    Target = savedAi.TargetStableId is { } targetId
                        ? context.World.ResolveStableId(stableIdRemap.Resolve(targetId) ?? targetId)
                        : null,
                    Destination = savedAi.DestinationX is { } destinationX && savedAi.DestinationY is { } destinationY
                        ? new OpenTK.Mathematics.Vector2i(destinationX, destinationY)
                        : null,
                    LastKnownPosition = savedAi.LastKnownX is { } lastKnownX && savedAi.LastKnownY is { } lastKnownY
                        ? new OpenTK.Mathematics.Vector2i(lastKnownX, lastKnownY)
                        : null,
                    AlertLevel = savedAi.AlertLevel,
                    TurnsSinceContact = savedAi.TurnsSinceContact
                };
                if (aiState.Target is { } target && !context.World.IsAlive(target))
                    aiState.Target = null;
                context.World.Set(entity, aiState);
            }
            if (savedEntity.Awareness is { } savedAwareness)
            {
                var awareness = Awareness.Create();
                foreach (var savedAwarenessEntry in savedAwareness)
                {
                    var target = context.World.ResolveStableId(
                        stableIdRemap.Resolve(savedAwarenessEntry.TargetStableId) ?? savedAwarenessEntry.TargetStableId);
                    if (!context.World.IsAlive(target))
                        continue;

                    awareness.Detected[target] = savedAwarenessEntry.Detected;
                    if (savedAwarenessEntry.LastKnownX is { } lastKnownX &&
                        savedAwarenessEntry.LastKnownY is { } lastKnownY)
                        awareness.LastKnownPositions[target] = new OpenTK.Mathematics.Vector2i(lastKnownX, lastKnownY);
                    awareness.TurnsSinceDetected[target] = savedAwarenessEntry.TurnsSinceDetected;
                }
                context.World.Set(entity, awareness);
            }
            if (savedEntity.Witnessed is { } witnessed && context.World.Has<WitnessMemory>(entity))
            {
                var memory = WitnessMemory.Create();
                memory.Witnessed.AddRange(witnessed.Select(savedEvent => new SimEvent(
                    savedEvent.Type,
                    savedEvent.Description,
                    savedEvent.MapId,
                    new OpenTK.Mathematics.Vector2i(savedEvent.X, savedEvent.Y),
                    savedEvent.Minute,
                    stableIdRemap.Resolve(savedEvent.ActorStableId),
                    stableIdRemap.Resolve(savedEvent.TargetStableId))));
                context.World.Set(entity, memory);
            }
            if (savedEntity.Wanted)
                context.World.Set(entity, new Wanted());
            else if (context.World.Has<Wanted>(entity))
                context.World.Remove<Wanted>(entity);
            if (savedEntity.Hostile)
                context.World.Set(entity, new Hostile());
            else if (context.World.Has<Hostile>(entity))
                context.World.Remove<Hostile>(entity);
            if (savedEntity.Searched)
                context.World.Set(entity, new Searched());
            else if (context.World.Has<Searched>(entity))
                context.World.Remove<Searched>(entity);
            if (savedEntity.LootGenerated)
                context.World.Set(entity, new LootGenerated());
            else if (context.World.Has<LootGenerated>(entity))
                context.World.Remove<LootGenerated>(entity);
            context.Scheduler.SetEnergy(entity, savedEntity.SchedulerEnergy);
            RestoreActivity(context, entity, savedEntity.Activity, stableIdRemap);
        }

        RestoreActivity(context, context.Player, save.PlayerActivity, stableIdRemap);

        if (save.WorldItems is { } savedItems)
        {
            foreach (var item in context.World.Query<ItemIdentity>()
                         .Where(item => context.World.Has<Location>(item) && !context.World.Has<InContainer>(item))
                         .ToList())
                context.World.Destroy(item);

            foreach (var savedItem in savedItems)
            {
                if (!context.Maps.Maps.ContainsKey(savedItem.MapId))
                    continue;

                var item = EntitySpawner.CreateItem(
                    context.World,
                    savedItem.MapId,
                    context.Definitions.Item(savedItem.DefinitionId),
                    savedItem.X,
                    savedItem.Y,
                    savedItem.Count,
                    savedItem.StableId);
                if (savedItem.StableId is { } savedId)
                    stableIdRemap.Add(savedId, item, context.World);
            }
        }

        foreach (var savedDoor in save.DoorStates ?? [])
        {
            var key = new DoorKey(
                savedDoor.FromMap,
                new OpenTK.Mathematics.Vector2i(savedDoor.FromX, savedDoor.FromY),
                savedDoor.ToMap,
                new OpenTK.Mathematics.Vector2i(savedDoor.ToX, savedDoor.ToY));
            if (!context.DoorStates.ContainsKey(key))
                continue;

            context.DoorStates[key] = new DoorState
            {
                Locked = savedDoor.Locked,
                Broken = savedDoor.Broken,
                TrespassReported = savedDoor.TrespassReported
            };
        }

        foreach (var item in context.World.Query<InContainer>().ToList())
        {
            var parent = context.World.Get<InContainer>(item).Parent.Resolve(context.World);
            if (context.World.IsAlive(parent) && !parent.Equals(context.Player))
                context.World.Remove<InContainer>(item);
        }

        foreach (var savedContainer in save.Containers ?? [])
        {
            var container = ResolveContainer(context, savedContainer);
            if (!context.World.IsAlive(container) || !context.World.Has<Container>(container))
                continue;
            stableIdRemap.Add(savedContainer.ContainerStableId, container, context.World);

            // Stable IDs from older saves can point to a different generated item.
            if (savedContainer.Items is null)
                continue;

            ref var state = ref context.World.Get<Container>(container);

            if (context.World.Has<LockState>(container))
            {
                ref var lockState = ref context.World.Get<LockState>(container);
                lockState.Locked = savedContainer.Locked;
                lockState.Broken = savedContainer.Broken;
            }

            foreach (var item in state.Items.Where(context.World.IsAlive).ToList())
                context.World.Destroy(item);

            state.Items = [];
            if (!context.World.Has<Position>(container) || !context.World.Has<Location>(container))
                continue;

            var containerPosition = context.World.Get<Position>(container).Value;
            var location = context.World.Get<Location>(container).MapId;
            for (var index = 0; index < savedContainer.Items.Count; index++)
            {
                var savedItem = savedContainer.Items[index];
                var item = EntitySpawner.CreateItem(
                    context.World,
                    location,
                    context.Definitions.Item(savedItem.DefinitionId),
                    (int)containerPosition.X,
                    (int)containerPosition.Y,
                    savedItem.Count,
                    savedItem.StableId ?? (index < savedContainer.ItemStableIds.Count
                        ? savedContainer.ItemStableIds[index]
                        : null));
                ItemSystem.Transfer(context.World, item, container);
                var savedItemId = savedItem.StableId ?? (index < savedContainer.ItemStableIds.Count
                    ? savedContainer.ItemStableIds[index]
                    : null);
                if (savedItemId is { } itemId && context.World.IsAlive(item))
                    stableIdRemap.Add(itemId, item, context.World);
            }
        }
    }

    private static CharacterData ToCharacterData(CharacterIdentity identity) =>
        new(
            identity.Name,
            identity.ProfessionId,
            identity.BackgroundId,
            new(identity.Stats, StringComparer.OrdinalIgnoreCase),
            [.. identity.TraitIds],
            [.. identity.SkillIds]);

    private static ActivityData? CaptureActivity(World world, Entity entity)
    {
        if (!world.Has<Activity>(entity))
            return null;

        var activity = world.Get<Activity>(entity);
        var target = activity.Target is { } reference &&
                     world.IsAlive(reference.Resolve(world))
            ? (long?)world.StableId(reference.Resolve(world))
            : null;
        return new ActivityData(activity.Kind, activity.RemainingMinutes, activity.TotalMinutes, target);
    }

    private static IReadOnlyList<WitnessEventData>? CaptureWitnessed(World world, Entity entity)
    {
        if (!world.Has<WitnessMemory>(entity)) return null;
        return world.Get<WitnessMemory>(entity).Witnessed.Select(simEvent => new WitnessEventData(
            simEvent.Type,
            simEvent.Description,
            simEvent.MapId,
            simEvent.Location.X,
            simEvent.Location.Y,
            simEvent.Minute,
            simEvent.ActorStableId,
            simEvent.TargetStableId)).ToList();
    }

    private static void RestoreActivity(
        GameContext context,
        Entity entity,
        ActivityData? saved,
        StableIdRemap stableIdRemap)
    {
        if (saved is null)
            return;

        StableEntityReference? target = null;
        if (saved.TargetStableId is { } targetId)
            target = new StableEntityReference(stableIdRemap.Resolve(targetId) ?? targetId);
        context.World.Set(entity, new Activity
        {
            Kind = saved.Kind,
            RemainingMinutes = saved.RemainingMinutes,
            TotalMinutes = saved.TotalMinutes,
            Target = target
        });
    }

    private static string? CurrentDefinitionId(World world, Entity entity) =>
        world.Has<CreatureIdentity>(entity)
            ? world.Get<CreatureIdentity>(entity).DefinitionId
            : world.Has<WorldObjectIdentity>(entity)
                ? world.Get<WorldObjectIdentity>(entity).DefinitionId
                : null;

    private static Entity ResolveWorldEntity(GameContext context, WorldEntityData saved)
    {
        if (saved.SaveId is { } saveId)
        {
            var persistent = context.World.Query<PersistentIdentity>()
                .FirstOrDefault(entity => context.World.Get<PersistentIdentity>(entity).Id == saveId);
            if (context.World.IsAlive(persistent) && MatchesDefinition(context.World, persistent, saved))
                return persistent;
        }

        var byStableId = context.World.ResolveStableId(saved.StableId);
        if (context.World.IsAlive(byStableId) && MatchesDefinition(context.World, byStableId, saved))
            return byStableId;

        if (saved.DefinitionId is not { } definitionId)
            return default;

        var existing = context.World.Query<Location, Position>()
            .Where(entity => !entity.Equals(context.Player) && !context.World.Has<ItemIdentity>(entity))
            .Where(entity => CurrentDefinitionId(context.World, entity) == definitionId)
            .Where(entity => saved.Name is null ||
                             (context.World.Has<Named>(entity) && context.World.Get<Named>(entity).Name == saved.Name))
            .Where(entity => saved.Name is not null ||
                             context.World.Get<Location>(entity).MapId == saved.MapId)
            .FirstOrDefault();
        if (context.World.IsAlive(existing))
            return existing;

        if (saved.DefinitionId is null)
            return default;
        if (saved.SpawnKind == "creature")
        {
            var creature = EntitySpawner.CreateHuman(
                context.World,
                saved.MapId,
                context.Definitions.Creature(saved.DefinitionId),
                saved.X,
                saved.Y,
                saved.Name ?? saved.DefinitionId,
                saved.SaveId,
                saved.StableId);
            return creature;
        }
        if (saved.SpawnKind == "object")
        {
            var worldObject = EntitySpawner.CreateWorldObject(
                context.World,
                saved.MapId,
                context.Definitions.WorldObject(saved.DefinitionId),
                saved.X,
                saved.Y,
                saved.SaveId,
                saved.StableId);
            return worldObject;
        }
        return default;
    }

    private static Entity ResolveContainer(GameContext context, ContainerStateData saved)
    {
        if (saved.SaveId is { } saveId)
        {
            var persistent = context.World.Query<PersistentIdentity>()
                .Where(entity => context.World.Has<Container>(entity))
                .FirstOrDefault(entity => context.World.Get<PersistentIdentity>(entity).Id == saveId);
            if (context.World.IsAlive(persistent) && MatchesContainerDefinition(context.World, persistent, saved))
                return persistent;
        }

        var byStableId = context.World.ResolveStableId(saved.ContainerStableId);
        if (context.World.IsAlive(byStableId) && MatchesContainerDefinition(context.World, byStableId, saved))
            return byStableId;
        if (saved.DefinitionId is not { } definitionId)
            return default;

        return context.World.Query<Container>()
            .Where(entity => context.World.Has<WorldObjectIdentity>(entity))
            .Where(entity => context.World.Get<WorldObjectIdentity>(entity).DefinitionId == definitionId)
            .FirstOrDefault();
    }

    private static bool MatchesDefinition(World world, Entity entity, WorldEntityData saved) =>
        saved.DefinitionId is null || CurrentDefinitionId(world, entity) == saved.DefinitionId;

    private static bool MatchesContainerDefinition(World world, Entity entity, ContainerStateData saved) =>
        saved.DefinitionId is null ||
        (world.Has<WorldObjectIdentity>(entity) &&
         world.Get<WorldObjectIdentity>(entity).DefinitionId == saved.DefinitionId);

    private static AiStateData? CaptureAiState(World world, Entity entity)
    {
        if (!world.Has<AIState>(entity))
            return null;

        var state = world.Get<AIState>(entity);
        return new AiStateData(
            state.Mode,
            state.Target is { } target && world.IsAlive(target) ? world.StableId(target) : null,
            state.Destination?.X,
            state.Destination?.Y,
            state.LastKnownPosition?.X,
            state.LastKnownPosition?.Y,
            state.AlertLevel,
            state.TurnsSinceContact);
    }

    private static IReadOnlyList<AwarenessData>? CaptureAwareness(World world, Entity entity)
    {
        if (!world.Has<Awareness>(entity))
            return null;

        var awareness = world.Get<Awareness>(entity);
        var targets = awareness.Detected.Keys
            .Concat(awareness.LastKnownPositions.Keys)
            .Concat(awareness.TurnsSinceDetected.Keys)
            .Distinct()
            .Where(world.IsAlive)
            .Select(target => new AwarenessData(
                world.StableId(target),
                awareness.Detected.GetValueOrDefault(target),
                awareness.LastKnownPositions.TryGetValue(target, out var position) ? position.X : null,
                awareness.LastKnownPositions.TryGetValue(target, out position) ? position.Y : null,
                awareness.TurnsSinceDetected.GetValueOrDefault(target)))
            .ToList();
        return targets;
    }
}
