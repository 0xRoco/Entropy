using OpenTK.Mathematics;

namespace Entropy.Engine.UI.Widgets;

public class BarWidget : Widget
{
    public string Label { get; set; } = "";
    public float Fraction { get; set; } 
    public string ValueText { get; set; } = "";

    public Color4 FillColor => Fraction > 0.6f ? UiTheme.BarGood
        : Fraction > 0.3f ? UiTheme.BarWarn 
        : UiTheme.BarBad;
    public const float BarThickness = 0.7f;

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var x = X + offsetX;
        var y = Y + offsetY;
        
        if (Label.Length > 0)
            context.DrawText(x, y, Label, UiTheme.Text);
        
        var labelWidth = Label.Length > 0 ? Label.Length + 1 : 0;
        var valueWidth = ValueText.Length > 0 ? ValueText.Length + 1 : 0;
        var barWidth = Width - labelWidth - valueWidth;
        if (barWidth <= 0) return;
        
        var barX = x + labelWidth;
        var centerY  = y + (1f - BarThickness) / 2f;
        
        context.DrawRect(barX, centerY, barWidth, BarThickness, UiTheme.BarTrack);
        
        var fraction = Math.Clamp(Fraction, 0f, 1f);
        if (fraction > 0f)
            context.DrawRect(barX, centerY, barWidth * Fraction, BarThickness, FillColor);
        
        base.Draw(context, offsetX, offsetY);
    }
}