using OpenTK.Mathematics;

namespace Entropy.Content;

public static class ColorNames
{
    private static readonly Dictionary<string, Color4> Table = new(StringComparer.OrdinalIgnoreCase)
    {
        ["white"] = Color4.White,
        ["black"] = Color4.Black,
        ["gray"] = Color4.Gray,
        ["light_gray"] = Color4.LightGray,
        ["dark_gray"] = Color4.DarkGray,
        ["red"] = Color4.Red,
        ["dark_red"] = new Color4(0.5f, 0f, 0f, 1f),
        ["green"] = Color4.Green,
        ["dark_green"] = new Color4(0f, 0.5f, 0f, 1f),
        ["blue"] = Color4.Blue,
        ["light_blue"] = new Color4(0.5f, 0.7f, 1f, 1f),
        ["yellow"] = Color4.Yellow,
        ["light_yellow"] = Color4.LightYellow,
        ["pink"] = Color4.Pink,
        ["orange"] = Color4.Orange,
        ["brown"] = new Color4(0.55f, 0.4f, 0.25f, 1f),
        ["cyan"] = Color4.Cyan,
        ["magenta"] = Color4.Magenta
    };

    public static IReadOnlyDictionary<string, Color4> ByName => Table;

    public static IReadOnlyList<string> Names { get; } = Table.Keys.OrderBy(k => k).ToList();

    public static bool TryParse(string name, out Color4 color) =>
        Table.TryGetValue(name, out color!);

    public static Color4 Parse(string name) => TryParse(name, out var color)
        ? color
        : throw new KeyNotFoundException($"Unknown color name '{name}'.");

    public static string? NameOf(Color4 color)
    {
        foreach (var (name, candidate) in Table)
        {
            if (Math.Abs(candidate.R - color.R) < 0.002f &&
                Math.Abs(candidate.G - color.G) < 0.002f &&
                Math.Abs(candidate.B - color.B) < 0.002f &&
                Math.Abs(candidate.A - color.A) < 0.002f)
            {
                return name;
            }
        }
        return null;
    }
}
