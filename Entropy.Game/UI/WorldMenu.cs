using Entropy.Engine.ECS;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class WorldMenu
{
    public bool IsOpen => _menu.IsOpen;
    public event Action<GameContext, Entity>? ContainerRequested;
    public event Action<ActionResult>? ActionCompleted;

    private readonly ContextMenu _menu;
    private List<WorldVerb> _verbs = [];
    private GameContext? _context;
    private Entity _player;

    public WorldMenu(Ui ui)
    {
        _menu = new ContextMenu(ui);
    }

    public void Open(GameContext context, Entity player, Vector2i tile, UiRect mapBounds)
    {
        _context = context;
        _player = player;

        _verbs = InteractionSystem.GetVerbs(context, player, tile,
            (ctx, container) => ContainerRequested?.Invoke(ctx, container));
        if (_verbs.Count == 0)
            return;

        var items = _verbs
            .Select(verb => (verb.Label, Run: (Action)(() => Execute(verb))))
            .ToList();

        var header = InteractionSystem.DescribeTargetName(context, tile);
        var width = Math.Max(16, header.Length + items.Max(i => i.Label.Length) + 10);
        var x = mapBounds.X + Math.Clamp(
            tile.X - mapBounds.X, 0, Math.Max(0, mapBounds.Width - width));
        var y = mapBounds.Y + Math.Clamp(
            tile.Y - mapBounds.Y, 0, Math.Max(0, mapBounds.Height - items.Count - 3));

        _menu.Show(x, y, header, items);
    }

    public bool HandleKey(Keys key) => _menu.HandleKey(key);

    private void Execute(WorldVerb verb)
    {
        var context = _context ?? throw new InvalidOperationException("World menu opened without a context.");
        ActionCompleted?.Invoke(verb.Execute(context, _player));
    }
}