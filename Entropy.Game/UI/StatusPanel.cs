using Entropy.Engine.ECS;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;

namespace Entropy.Game.UI;

public class StatusPanel : Panel
{
    private readonly BarWidget _healthBar;
    private readonly BarWidget _hungerBar;
    private readonly BarWidget _thirstBar;
    private readonly BarWidget _energyBar;

    private readonly Label _wieldLabel;
    private readonly Label _wantedLabel;

    private readonly World _world;
    private readonly Entity _player;

    public StatusPanel(World world, Entity player, int seed)
    {
        _world = world;
        _player = player;
        Height = 15;

        const int innerX = 1;
        Add(new Label { X = innerX, Y = 1, Width = 22, Text = "STATUS", Color = UiTheme.Keybind });
        _healthBar = new BarWidget { X = innerX, Y = 3, Width = 22, Label = "HP" };
        Add(_healthBar);

        _hungerBar = new BarWidget { X = innerX, Y = 5, Width = 22, Label = "Food" };
        Add(_hungerBar);

        _thirstBar = new BarWidget { X = innerX, Y = 7, Width = 22, Label = "Water" };
        Add(_thirstBar);

        _energyBar = new BarWidget { X = innerX, Y = 9, Width = 22, Label = "Energy" };
        Add(_energyBar);

        _wieldLabel = new Label { X = innerX, Y = 11, Width = 22 };
        Add(_wieldLabel);

        Add(new Label { X = innerX, Y = 12, Width = 22, Text = $"Seed: {seed}", Color = UiTheme.TextDim });
        _wantedLabel = new Label { X = innerX, Y = 13, Width = 22 };
        Add(_wantedLabel);
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        if (_world.IsAlive(_player) && _world.Has<Health>(_player))
        {
            ref var hp = ref _world.Get<Health>(_player);
            _healthBar.Fraction = hp.Max > 0 ? hp.Current / (float)hp.Max : 0f;
            _healthBar.ValueText = $"{hp.Current}/{hp.Max}";

            if (_world.Has<Hunger>(_player))
            {
                ref var h = ref _world.Get<Hunger>(_player);
                _hungerBar.Fraction = h.Max > 0 ? h.Current / (float)h.Max : 0f;
                _hungerBar.ValueText = $"{h.Current}/{h.Max}";
            }


            if (_world.Has<Thirst>(_player))
            {
                ref var t = ref _world.Get<Thirst>(_player);
                _thirstBar.Fraction = t.Max > 0 ? t.Current / (float)t.Max : 0f;
                _thirstBar.ValueText = $"{t.Current}/{t.Max}";
            }

            if (_world.Has<Fatigue>(_player))
            {
                ref var f = ref _world.Get<Fatigue>(_player);
                _energyBar.Fraction = f.Max > 0 ? f.Current / (float)f.Max : 0f;
                _energyBar.ValueText = $"{f.Current}/{f.Max}";
            }
        }

        _wieldLabel.Text = _world.Has<Equipped>(_player)
            ? $"Wielded: {_world.Get<ItemIdentity>(_world.Get<Equipped>(_player).Item).Name}"
            : "Wielded: Fists";

        _wantedLabel.Text = _world.Has<Wanted>(_player) ? "WANTED" : "";
        _wantedLabel.Color = _world.Has<Wanted>(_player) ? UiTheme.Danger : UiTheme.Text;

        base.Draw(context, offsetX, offsetY);
    }
}
