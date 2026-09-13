using OpenTK.Mathematics;

namespace Entropy.Engine.UI;

public static class UiTheme
{
    public static readonly Color4 Text = new(0.72f, 0.72f, 0.72f, 1f);
    public static readonly Color4 TextBright = new(0.94f, 0.94f, 0.94f, 1f);
    public static readonly Color4 TextDim = new(0.42f, 0.42f, 0.42f, 1f);
    
    public static readonly Color4 Selection = new(0.08f, 0.20f, 0.62f, 1f);

    public static readonly Color4 Keybind = new(1f, 0.92f, 0.1f, 1f);
    public static readonly Color4 Valid = new(0.15f, 0.85f, 0.25f, 1f);
    public static readonly Color4 Danger = new(0.95f, 0.15f, 0.2f, 1f);
    public static readonly Color4 Info = new(0.1f, 0.85f, 0.9f, 1f);
    public static readonly Color4 Heading = new(0.85f, 0.2f, 0.55f, 1f);
    public static readonly Color4 Border = new(0.68f, 0.68f, 0.68f, 1f);

    public static readonly Color4 BarGood = Valid;
    public static readonly Color4 BarWarn = Color4.Yellow;
    public static readonly Color4 BarBad = Danger;
    public static readonly Color4 BarTrack = new(0.15f, 0.15f, 0.2f, 1f);

    public static readonly Color4 PanelBackground = new(0.015f, 0.015f, 0.02f, 1f);
    public static readonly Color4 PanelBorder = Border;

    public static readonly Color4 LogBackground = PanelBackground;
    public static readonly Color4 LogBorder = TextDim;

}
