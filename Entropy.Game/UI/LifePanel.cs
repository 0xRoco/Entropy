using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;

namespace Entropy.Game.UI;

public class LifePanel : Panel
{
    public LifePanel(int seed)
    {
        Height = 4;
        Add(new Label { X = 1, Y = 1, Width = 22, Text = "LIFE", Color = UiTheme.Keybind });
        Add(new Label { X = 1, Y = 2, Width = 22, Text = $"Seed: {seed}" });
    }
}