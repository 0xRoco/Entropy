using Entropy.Engine.ECS;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;

namespace Entropy.Game.UI;

public class StatusPanel : Panel
{
    private readonly BarWidget _healthBar;
    private readonly Label _wieldLabel;

    private readonly World _world;
    private readonly Entity _player;

    public StatusPanel(World world, Entity player)
    {
        _world = world;
        _player = player;
        Height = 7;

        const int innerX = 1;
        Add(new Label { X = innerX, Y = 1, Width = 22, Text = "BODY", Color = UiTheme.Keybind });
        _healthBar = new BarWidget { X = innerX, Y = 3, Width = 22, Label = "Health" };
        Add(_healthBar);
        _wieldLabel = new Label { X = innerX, Y = 5, Width = 22 };
        Add(_wieldLabel);
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        if (_world.IsAlive(_player) && _world.Has<Health>(_player))
        {
            ref var hp = ref _world.Get<Health>(_player);
            _healthBar.Fraction = hp.Max > 0 ? hp.Current / (float)hp.Max : 0f;
            _healthBar.ValueText = $"{hp.Current}/{hp.Max}";
        }

        _wieldLabel.Text = _world.Has<Equipped>(_player)
            ? $"Weapon: {_world.Get<ItemIdentity>(_world.Get<Equipped>(_player).Item).Name}"
            : "Weapon: Fists";

        base.Draw(context, offsetX, offsetY);
    }
}