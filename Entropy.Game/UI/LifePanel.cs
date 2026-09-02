using Entropy.Engine.ECS;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;

namespace Entropy.Game.UI;

public class LifePanel : Panel
{
    private readonly Label _wantedLabel;
    private readonly World _world;
    private readonly Entity _player;

    public LifePanel(int seed, World world, Entity player)
    {
        _world = world;
        _player = player;
        Height = 5;

        Add(new Label { X = 1, Y = 1, Width = 22, Text = "LIFE", Color = UiTheme.Keybind });
        Add(new Label { X = 1, Y = 2, Width = 22, Text = $"Seed: {seed}" });
        _wantedLabel = new Label { X = 1, Y = 3, Width = 22 };
        Add(_wantedLabel);
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        _wantedLabel.Text = _world.Has<Wanted>(_player) ? "WANTED" : "";
        _wantedLabel.Color = _world.Has<Wanted>(_player) ? UiTheme.Danger : UiTheme.Text;
        base.Draw(context, offsetX, offsetY);
    }
}