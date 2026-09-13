using Entropy.Engine.ECS;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;
using Entropy.Game.Components.Vitals;

namespace Entropy.Game.UI;

public class BodyPanel : Panel
{
    private static readonly string[] Parts = ["Head", "Torso", "L Arm", "R Arm", "L Leg", "R Leg"];

    private readonly Label[] _rows;
    private readonly World _world;
    private readonly Entity _player;

    public BodyPanel(World world, Entity player)
    {
        _world = world;
        _player = player;
        Height = 9;

        Add(new Label { X = 1, Y = 1, Width = 22, Text = "BODY", Color = UiTheme.Keybind });
        _rows =
        [
            .. Parts.Select((part, i) =>
            {
                var label = new Label { X = 1, Y = 2 + i, Width = 22 };
                Add(label);
                return label;
            })
        ];
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var fraction = 1f;
        if (_world.IsAlive(_player) && _world.Has<Health>(_player))
        {
            ref var hp = ref _world.Get<Health>(_player);
            fraction = hp.Max > 0 ? hp.Current / (float)hp.Max : 0f;
        }

        for (var i = 0; i < Parts.Length; i++)
        {
            var blocks = (int)MathF.Round(fraction * 6);
            _rows[i].Text = $"{Parts[i],-6} {new string('|', blocks),-6}";
            _rows[i].Color = fraction > 0.66f ? UiTheme.Valid
                : fraction > 0.33f ? UiTheme.BarWarn
                : UiTheme.Danger;
        }

        base.Draw(context, offsetX, offsetY);
    }
}