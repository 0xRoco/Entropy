using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Systems;
using OpenTK.Mathematics;

namespace Entropy.Game.UI;

public class CompassPanel : Panel
{
    private static readonly (string Label, Vector2i Offset)[] Directions =
    [
        ("N ", new Vector2i(0, -1)), ("NE", new Vector2i(1, -1)),
        ("E ", new Vector2i(1, 0)), ("SE", new Vector2i(1, 1)),
        ("S ", new Vector2i(0, 1)), ("SW", new Vector2i(-1, 1)),
        ("W ", new Vector2i(-1, 0)), ("NW", new Vector2i(-1, -1)),
    ];

    private readonly GameContext _context;
    private readonly Label[] _rows;

    public CompassPanel(GameContext context)
    {
        _context = context;
        Height = 12;

        Add(new Label { X = 1, Y = 1, Width = 22, Text = "COMPASS", Color = UiTheme.Keybind });
        _rows =
        [
            .. Directions.Select((_, i) =>
            {
                var label = new Label { X = 1, Y = 2 + i, Width = 22 };
                Add(label);
                return label;
            })
        ];
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var world = _context.World;
        if (!world.IsAlive(_context.Player) || !world.Has<Position>(_context.Player))
        {
            base.Draw(context, offsetX, offsetY);
            return;
        }

        var position = world.Get<Position>(_context.Player).Value;

        for (var i = 0; i < Directions.Length; i++)
        {
            var (label, offset) = Directions[i];
            var tile = new Vector2i(
                (int)position.X + offset.X,
                (int)position.Y + offset.Y);

            var inBounds = tile.X >= 0 && tile.X < _context.Map.Width &&
                           tile.Y >= 0 && tile.Y < _context.Map.Height;
            var name = inBounds
                ? InteractionSystem.DescribeTargetName(_context, tile)
                : string.Empty;

            _rows[i].Text = name.Length == 0 ? label : $"{label}: {name}";
        }

        base.Draw(context, offsetX, offsetY);
    }
}