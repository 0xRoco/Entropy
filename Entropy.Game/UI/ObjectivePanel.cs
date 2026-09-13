using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;

namespace Entropy.Game.UI;

public sealed class ObjectivePanel : Panel
{
    private readonly GameContext _context;
    private readonly Label _objective;

    public ObjectivePanel(GameContext context)
    {
        _context = context;
        Height = 4;
        Add(new Label { X = 1, Y = 1, Width = 22, Text = "OBJECTIVE", Color = UiTheme.Keybind });
        _objective = new Label { X = 1, Y = 2, Width = 22 };
        Add(_objective);
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        _objective.Text = _context.Objective.Description;
        _objective.Color = _context.Objective.Complete ? UiTheme.Valid : UiTheme.Text;
        base.Draw(context, offsetX, offsetY);
    }
}
