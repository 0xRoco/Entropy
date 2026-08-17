using Entropy.Engine.Rendering;
using OpenTK.Mathematics;

namespace Entropy.Game.UI;

public static class LogPanel
{
    public const int VisibleLines = 6;
    public const float CharSize = 16f;
    public const float Margin = 8f;

    public static void Draw(MessageLog log, QuadBatcher batcher, UiCamera camera)
    {
        var lines = log.GetRecent(VisibleLines);
        if (lines.Count == 0) return;

        var logHeight = lines.Count * CharSize;
        var startY = camera.ViewportSize.Y - logHeight - Margin;

        batcher.AddRect(0, startY - 4, camera.ViewportSize.X, logHeight + 8,
            new Color4(0.1f, 0.1f, 0.15f, 0.85f));

        for (var i = 0; i < lines.Count; i++)
        {
            var (text, color) = lines[i];
            var age = lines.Count - 1 - i;
            var dim = MathF.Max(0.35f, 1f - age * 0.15f);
            var lineColor = color.Scaled(dim);
            var y = startY + i * CharSize;

            for (var j = 0; j < text.Length; j++)
                batcher.AddTexturedQuad(Margin + j * CharSize, y, CharSize, CharSize, lineColor, text[j]);
        }
        batcher.Flush();
    }
}