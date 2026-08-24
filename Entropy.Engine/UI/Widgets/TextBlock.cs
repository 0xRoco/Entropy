using OpenTK.Mathematics;

namespace Entropy.Engine.UI.Widgets;

public class TextBlock : Widget
{
    public string Text { get; set; } = "";
    public Color4 Color { get; set; } = Color4.White;

    public IReadOnlyList<string> Lines => WrapText();

    public int LineCount => Lines.Count;

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var x = X + offsetX;
        var y = Y + offsetY;

        var lines = WrapText();

        for (var row = 0; row < lines.Count; row++)
            context.DrawText(x, y + row, lines[row], Color);
    }

    private List<string> WrapText()
    {
        var width = Math.Max(1, Width);
        var result = new List<string>();

        foreach (var paragraph in Text.Replace("\r\n", "\n").Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                result.Add("");
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = "";

            foreach (var word in words)
            {
                // Long unbroken text still needs to fit.
                if (word.Length > width)
                {
                    if (line.Length > 0)
                    {
                        result.Add(line);
                        line = "";
                    }

                    for (var start = 0; start < word.Length; start += width)
                        result.Add(word.Substring(start, Math.Min(width, word.Length - start)));

                    continue;
                }

                var candidate = line.Length == 0 ? word : $"{line} {word}";

                if (candidate.Length <= width)
                {
                    line = candidate;
                    continue;
                }

                result.Add(line);
                line = word;
            }

            if (line.Length > 0)
                result.Add(line);
        }

        return result;
    }
}