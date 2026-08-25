using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using OpenTK.Mathematics;

namespace Entropy.Game.UI;

public class MessageLogPanel : Widget
{
    public MessageLog Log { get; set; } = null!;
    public float FadePerLines { get; set; } = 0.15f;
    public float MinBrightness { get; set; } = 0.35f;
    
    private const int VisibleLines = 6;
    private readonly List<Label> _labels = [];

    public MessageLogPanel()
    {
        for (var i = 0; i < VisibleLines; i++)
        {
            var label = new Label { X = 1, Y = 1 + i };
            _labels.Add(label);
            Add(label);
        }
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var maxLines = Math.Min(VisibleLines, Math.Max(0, Height - 2));
        var lines = Log.GetRecent(maxLines) ?? [];

        for (var i = 0; i < _labels.Count; i++)
        {
            var hasLine = i < lines.Count;
            _labels[i].Visible = hasLine;
            if (!hasLine) continue;
            
            var (text, color) = lines[i];
            var age = lines.Count - 1 - i; // 0 is newest
            var dim = MathF.Max(MinBrightness, 1f - age * FadePerLines);
            
            _labels[i].Text = text;
            _labels[i].Width = Width - 2; // interior width
            _labels[i].Color = new Color4(color.R * dim, color.G * dim, color.B * dim, color.A);
        }

        var x = X + offsetX;
        var y = Y + offsetY;
        context.DrawRect(x, y, Width, Height, UiTheme.LogBackground);
        context.DrawBorder(x, y, Width, Height, UiTheme.LogBorder);

        base.Draw(context, offsetX, offsetY);
    }
}