using OpenTK.Mathematics;

namespace Entropy.Engine.UI;

public static class UiTheme
{
    public static readonly Color4 Text = new(0.82f, 0.82f, 0.82f, 1f);      
    public static readonly Color4 TextBright = Color4.White;
    public static readonly Color4 TextDim = new(0.45f, 0.45f, 0.45f, 1f);
    
    public static readonly Color4 Selection = new(0.16f, 0.30f, 0.55f, 1f);  

    public static readonly Color4 Keybind = Color4.Yellow;
    public static readonly Color4 Valid = Color4.Green;
    public static readonly Color4 Danger = Color4.Red;
    public static readonly Color4 Info = Color4.Cyan;

    public static readonly Color4 BarGood = Valid;
    public static readonly Color4 BarWarn = Color4.Yellow;
    public static readonly Color4 BarBad = Danger;
    public static readonly Color4 BarTrack = new(0.15f, 0.15f, 0.2f, 1f);

    public static readonly Color4 PanelBackground = Color4.Black;
    public static readonly Color4 PanelBorder = TextDim;

    public static readonly Color4 LogBackground = PanelBackground;
    public static readonly Color4 LogBorder = new(0.3f, 0.3f, 0.35f, 0.8f);

}