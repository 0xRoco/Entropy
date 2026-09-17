namespace Entropy.Simulation;

public static class MapDefinitionValidator
{
    public static IReadOnlyList<string> Validate(MapDefinition definition)
    {
        var errors = new List<string>();
        if (definition.Version != 1)
            errors.Add($"unsupported version {definition.Version}");
        if (string.IsNullOrWhiteSpace(definition.Id))
            errors.Add("map id is required");
        if (definition.Width <= 0 || definition.Height <= 0)
            errors.Add("map dimensions must be positive");
        if (definition.Terrain is null)
            return [.. errors, "terrain layer is required"];
        if (definition.Terrain.Rows.Count != definition.Height)
            errors.Add($"terrain has {definition.Terrain.Rows.Count} rows, expected {definition.Height}");

        var anchors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var anchor in definition.Anchors)
        {
            if (!anchors.Add(anchor.Id))
                errors.Add($"duplicate anchor '{anchor.Id}'");
            ValidateCoordinate(errors, $"anchor '{anchor.Id}'", anchor.X, anchor.Y, definition.Width, definition.Height);
        }

        var objects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var placement in definition.Objects)
        {
            if (!objects.Add(placement.Id))
                errors.Add($"duplicate object '{placement.Id}'");
            if (string.IsNullOrWhiteSpace(placement.Definition))
                errors.Add($"object '{placement.Id}' definition is required");
            ValidateCoordinate(errors, $"object '{placement.Id}'", placement.X, placement.Y, definition.Width, definition.Height);
        }

        var transitions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var transition in definition.Transitions)
        {
            if (!transitions.Add(transition.Id))
                errors.Add($"duplicate transition '{transition.Id}'");
            if (string.IsNullOrWhiteSpace(transition.TargetMap))
                errors.Add($"transition '{transition.Id}' target map is required");
            if (string.IsNullOrWhiteSpace(transition.TargetAnchor))
                errors.Add($"transition '{transition.Id}' target anchor is required");
            ValidateCoordinate(errors, $"transition '{transition.Id}'", transition.X, transition.Y, definition.Width, definition.Height);
        }

        for (var y = 0; y < Math.Min(definition.Terrain.Rows.Count, definition.Height); y++)
        {
            var row = definition.Terrain.Rows[y];
            if (row.Length != definition.Width)
                errors.Add($"terrain row {y} has length {row.Length}, expected {definition.Width}");
            foreach (var symbol in row.Distinct())
                if (!definition.Terrain.Legend.ContainsKey(symbol.ToString()))
                    errors.Add($"terrain row {y} uses undefined symbol '{symbol}'");
        }

        return errors;
    }

    private static void ValidateCoordinate(List<string> errors, string label, int x, int y, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            errors.Add($"{label} coordinate ({x}, {y}) is outside {width}x{height}");
    }
}
