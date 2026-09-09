using System.Globalization;
using System.Windows.Data;
using Entropy.Content;
using OpenTK.Mathematics;

namespace Entropy.Editor;

/// <summary>Char to and from string for symbol fields.</summary>
public class CharConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is char c ? c.ToString() : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && s.Length == 1 ? s[0] : '\0';
}

/// <summary>Color4 to and from named color.</summary>
public class ColorNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Color4 color ? ColorNames.NameOf(color) ?? string.Empty : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string name && ColorNames.TryParse(name, out var color) ? color : Color4.Black;
}

/// <summary>Material list to and from "Cotton, Iron".</summary>
public class MaterialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IEnumerable<Material> materials
            ? string.Join(", ", materials.Select(m => m.ToString()))
            : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = new List<Material>();
        if (value is not string text) return result;

        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<Material>(part, ignoreCase: true, out var material))
                result.Add(material);
        }
        return result;
    }
}

/// <summary>Color4 to SolidColorBrush for live glyph previews.</summary>
public class Color4ToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Color4 c
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(
                (byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255)))
            : System.Windows.Media.Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>List of item ids to and from "crackers, canned_beans".</summary>
public class ItemIdsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IEnumerable<string> ids ? string.Join(", ", ids) : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = new List<string>();
        if (value is not string text) return result;

        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            result.Add(part);
        return result;
    }
}

/// <summary>string hashset to and from "soft, rigid".</summary>
public class FlagsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IEnumerable<string> flags ? string.Join(", ", flags) : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (value is not string text) return result;

        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            result.Add(part);

        return result;
    }
}
