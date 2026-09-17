using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.Rendering;
using Entropy.Engine.World;
using Entropy.Game.Components.AI;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using Entropy.Game.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.Systems;

public sealed class PlayerActions
{
    private readonly GameContext _context;
    private readonly ISimulation _simulation;
    private readonly IGameInput _input;
    private readonly Camera _camera;
    private readonly GameHud _hud;

    public PlayerActions(GameContext context, ISimulation simulation, IGameInput input, Camera camera, GameHud hud)
    {
        _context = context;
        _simulation = simulation;
        _input = input;
        _camera = camera;
        _hud = hud;
    }

    public void ProcessActivity()
    {
        if (!_context.World.IsAlive(_context.Player) || (_context.World.Has<Health>(_context.Player) && _context.World.Get<Health>(_context.Player).Current <= 0))
        {
            ActivitySystem.Cancel(_context, _context.Player, "Your activity is interrupted.");
            return;
        }
        AdvanceTurn();
        if (ActivitySystem.IsActive(_context.World, _context.Player) && _input.GetKeyPressed() != null)
            ActivitySystem.Interrupt(_context, _context.Player, "You wake up.");
    }

    public void ProcessActionResult(ActionResult action) => ActionScheduler.Process(action, AdvanceTurn);

    public ActionResult ProcessPlayerAction()
    {
        var move = Controls.GetMoveDirection(_input);
        if (move != null) return _simulation.Execute(new MoveCommand((Vector2i)move));
        return _input.GetKeyPressed() switch
        {
            Keys.G => _simulation.Execute(new PickupCommand()),
            Keys.E => OpenInteractMenu() ? ActionResult.Turn : ActionResult.Failed,
            Keys.Period => _simulation.Execute(new WaitCommand()),
            _ => ActionResult.Failed
        };
    }

    public void AdvanceTurn(int timeCostMinutes = 1)
    {
        _simulation.Advance(timeCostMinutes);
        if (_context.World.IsAlive(_context.Player) && _context.World.Has<Position>(_context.Player))
            _camera.Position = _context.World.Get<Position>(_context.Player).Value;
    }

    private bool OpenInteractMenu()
    {
        if (_hud.HasOpenModal) return false;
        var pos = _context.World.Get<Position>(_context.Player).Value;
        var facing = _context.World.Has<Facing>(_context.Player) ? _context.World.Get<Facing>(_context.Player).Direction : new Vector2i(1, 0);
        var target = new Vector2i((int)pos.X + facing.X, (int)pos.Y + facing.Y);
        if (target.X < 0 || target.X >= _context.Map.Width || target.Y < 0 || target.Y >= _context.Map.Height)
            target = new Vector2i((int)pos.X, (int)pos.Y);
        _hud.OpenWorldMenu(target);
        return false;
    }
    public static ActionResult Execute(SimulationCommand command, IGameRuntimeContext context, SimulationState state)
    {
        var player = state.Player;
        return command switch
        {
            MoveCommand move => Move(player, move.Direction, context, context.Visibility, context.ViewRadius, state.Scheduler),
            PickupCommand => Pickup(player, context),
            WaitCommand => ActionResult.Turn,
            ExamineCommand examineTile => Examine(examineTile.Tile, context),
            ExamineEntityCommand examineEntity => ExamineEntity(examineEntity.Target, context),
            PickupItemCommand pickup => InteractionSystem.PickUp(context, player, pickup.Item),
            TalkCommand talk => Talk(talk.Target, context),
            SearchCommand search => SearchSystem.Start(context, player, search.Container),
            SleepCommand sleep => SleepSystem.FallAsleep(context, player, sleep.Bed),
            AttackCommand attack => InteractionSystem.Attack(context, player, attack.Target),
            PurchaseCommand purchase => PurchaseSystem.TryPurchase(context, player, purchase.Item, purchase.Shop) ? ActionResult.Turn : ActionResult.Failed,
            StealCommand steal => PurchaseSystem.TrySteal(context, player, steal.Item, steal.Shop) ? ActionResult.Turn : ActionResult.Failed,
            UnlockDoorCommand unlockDoor => DoorSystem.Unlock(context, player, unlockDoor.Transition),
            ForceDoorCommand forceDoor => DoorSystem.ForceEntry(context, player, forceDoor.Transition, forceDoor.Source, forceDoor.ToolFlag, forceDoor.Method, forceDoor.NoiseRadius),
            UnlockContainerCommand unlockContainer => DoorSystem.UnlockContainer(context, player, unlockContainer.Container),
            ForceContainerCommand forceContainer => DoorSystem.ForceContainerEntry(context, player, forceContainer.Container, forceContainer.Source, forceContainer.ToolFlag, forceContainer.Method, forceContainer.NoiseRadius),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
    }

    public static ActionResult Move(Entity player, Vector2i move, IGameRuntimeContext context, VisibilityMap visibility, int viewRadius, ActorScheduler scheduler)
    {
        ref var pos = ref context.World.Get<Position>(player);
        var target = pos.Value + move;
        var tx = (int)target.X;
        var ty = (int)target.Y;
        if (tx < 0 || tx >= context.Map.Width || ty < 0 || ty >= context.Map.Height || !context.Map[tx, ty].Walkable)
            return ActionResult.Failed;

        var playerMapId = context.World.Get<Location>(player).MapId;
        var playerNameOf = (Entity entity) => context.World.Has<Named>(entity) ? context.World.Get<Named>(entity).Name : "something";
        foreach (var entity in context.World.Query<Position, Actor>())
        {
            if (entity.Equals(player) || context.World.Get<Position>(entity).Value != target ||
                !context.World.Has<Location>(entity) || context.World.Get<Location>(entity).MapId != playerMapId) continue;
            if (context.World.Has<Hostile>(entity))
            {
                InteractionSystem.Attack(context, player, entity);
                scheduler.Spend(player, 100);
                return ActionResult.Turn;
            }
            PublishBlocked(context, player, entity, tx, ty, playerNameOf(entity));
            scheduler.Spend(player, 100);
            return ActionResult.Turn;
        }

        foreach (var entity in context.World.Query<Position, Solid>())
        {
            if (!context.World.Get<Solid>(entity).Blocks || context.World.Get<Position>(entity).Value != target ||
                !context.World.Has<Location>(entity) || context.World.Get<Location>(entity).MapId != playerMapId) continue;
            PublishBlocked(context, player, entity, tx, ty, playerNameOf(entity));
            scheduler.Spend(player, 100);
            return ActionResult.Turn;
        }

        var transition = context.Maps.TransitionAt(playerMapId, new Vector2i(tx, ty));
        if (transition is not null)
        {
            if (!DoorSystem.IsPassable(context, transition))
            {
                context.Events.Publish(new SimEvent("door.locked", "The door is locked.", playerMapId, new Vector2i(tx, ty), context.Clock.MinuteOfDay, context.World.StableId(player)));
                return ActionResult.Failed;
            }
            pos.Value = target;
            context.World.Set(player, new Facing { Direction = new Vector2i(move.X, move.Y) });
            pos.Value = new Vector2(transition.ToTile.X, transition.ToTile.Y);
            context.World.Set(player, new Location { MapId = transition.ToMap });
            context.MapId = transition.ToMap;
            context.Map = context.Maps[transition.ToMap];
            context.Visibility = context.Visibilities[transition.ToMap];
            if (transition.ToMap.Equals("spire_commons", StringComparison.OrdinalIgnoreCase))
            {
                context.Arrival.HasEnteredSpire = true;
                context.Arrival.EntryRestricted = false;
                context.Arrival.LegalIdentityStatus = "temporary_resident";
                context.Events.Publish(new SimEvent(
                    "arrival.entered",
                    "The gate opens. You are admitted under temporary status. Find work or a sponsor before the permit expires.",
                    transition.ToMap,
                    transition.ToTile,
                    context.Clock.MinuteOfDay,
                    context.World.StableId(player)));
            }
            if (DoorSystem.TryGet(context, transition, out var door, out var doorState) && door.Trespass && doorState.Broken && !doorState.TrespassReported)
            {
                doorState.TrespassReported = true;
                context.DoorStates[DoorSystem.KeyFor(transition)] = doorState;
                var victim = context.World.Query<Home>().FirstOrDefault(entity => context.World.Has<Home>(entity) && context.World.Get<Home>(entity).MapId == transition.ToMap);
                ConsequenceSystem.Report(context, "trespass", "someone forced entry into a home", transition.ToTile, context.Player, victim);
            }
            Fov.Compute(transition.ToTile, viewRadius, context.Map, context.Visibility);
            scheduler.Spend(player, context.Map[transition.ToTile.X, transition.ToTile.Y].MoveCost);
            return ActionResult.Turn;
        }

        pos.Value = target;
        context.World.Set(player, new Facing { Direction = new Vector2i(move.X, move.Y) });
        Fov.Compute(new Vector2i(tx, ty), viewRadius, context.Map, visibility);
        scheduler.Spend(player, context.Map[tx, ty].MoveCost);
        return ActionResult.Turn;
    }

    public static ActionResult Pickup(Entity player, IGameRuntimeContext context)
    {
        if (!context.World.Has<Position>(player) || !context.World.Has<Location>(player)) return ActionResult.Failed;
        var position = context.World.Get<Position>(player).Value;
        var items = ItemSystem.ItemsAt(context.World, context.MapId, position);
        var pickedUp = false;
        foreach (var item in items)
        {
            if (!context.World.IsAlive(item) || !context.World.Has<ItemIdentity>(item) || !ItemSystem.TryPickup(context.World, player, item)) continue;
            context.Events.Publish(new SimEvent("item.picked_up", $"You pick up the {context.World.Get<ItemIdentity>(item).Name}.", context.MapId, new Vector2i((int)position.X, (int)position.Y), context.Clock.MinuteOfDay, context.World.StableId(player), context.World.IsAlive(item) ? context.World.StableId(item) : null));
            pickedUp = true;
        }
        return pickedUp ? ActionResult.Turn : ActionResult.Failed;
    }

    private static ActionResult Examine(Vector2i tile, IGameRuntimeContext context)
    {
        if (tile.X < 0 || tile.X >= context.Map.Width || tile.Y < 0 || tile.Y >= context.Map.Height) return ActionResult.Failed;
        InteractionSystem.ExamineAt(context, tile);
        return ActionResult.Free;
    }

    private static ActionResult ExamineEntity(Entity target, IGameRuntimeContext context)
    {
        if (!context.World.IsAlive(target)) return ActionResult.Failed;
        InteractionSystem.ExamineEntity(context, target);
        return ActionResult.Free;
    }

    private static ActionResult Talk(Entity target, IGameRuntimeContext context)
    {
        if (!context.World.IsAlive(target)) return ActionResult.Failed;
        var name = context.World.Has<Named>(target) ? context.World.Get<Named>(target).Name : "someone";
        var position = context.World.Get<Position>(target).Value;
        context.Events.Publish(new SimEvent("interaction.talk", $"{name} ignores you.", context.MapId, new Vector2i((int)position.X, (int)position.Y), context.Clock.MinuteOfDay, context.World.StableId(context.Player), context.World.StableId(target)));
        return ActionResult.Free;
    }

    private static void PublishBlocked(IGameRuntimeContext context, Entity player, Entity blocker, int x, int y, string name) =>
        context.Events.Publish(new SimEvent("movement.blocked", $"The {name} blocks your way", context.World.Get<Location>(player).MapId, new Vector2i(x, y), context.Clock.MinuteOfDay, context.World.StableId(player), context.World.IsAlive(blocker) ? context.World.StableId(blocker) : null));
}
