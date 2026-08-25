using Entropy.Engine.ECS;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;

namespace Entropy.Game.UI;

public class StatusPanel : Panel
{
    private readonly Label _header;
    private readonly BarWidget _healthBar;
    private readonly Label _wieldLabel;
    private readonly Label _turnLabel;
    private readonly Label _seedLabel;
    
    private readonly World _world;
    private readonly Entity _player;
    private readonly Func<int> _turnCount;

    public StatusPanel(World world, Entity player, Func<int> turnCount, int seed)
    {
        _world = world;
        _player = player;
        _turnCount = turnCount;
        
        const int innerX = 1;
        
        _header = new Label { X = innerX, Y = 1, Width = 22, Text = "STATUS", Color = UiTheme.Keybind };
        Add(_header);
        
        _healthBar = new BarWidget { X = innerX, Y = 3, Width = 22, Label = "Health" };
        Add(_healthBar);
        
        _wieldLabel = new Label { X = innerX, Y = 7, Width = 22, Text = "Wielding: Fists" };
        Add(_wieldLabel);
        
        _turnLabel = new Label { X = innerX, Y = 9, Width = 22, Text = $"Turn: {_turnCount()}" };
        Add(_turnLabel);
        
        _seedLabel = new Label { X = innerX, Y = 11, Width = 22, Text = $"Seed: {seed}" };
        Add(_seedLabel);
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        if (_world.IsAlive(_player))
        {
            if (_world.Has<Health>(_player))
            {
                ref var hp = ref _world.Get<Health>(_player);
                _healthBar.Fraction = hp.Max > 0 ? hp.Current / (float)hp.Max : 0f;
                _healthBar.ValueText = $"{hp.Current}/{hp.Max}";
            }
        }
        
        _wieldLabel.Text = _world.Has<Equipped>(_player)
            ? $"Wielding: {_world.Get<ItemIdentity>(_world.Get<Equipped>(_player).Item).Name}"
            : "Wielding: Fists";
        
        _turnLabel.Text = $"Turn: {_turnCount()}";
        
        base.Draw(context, offsetX, offsetY);
    }
}