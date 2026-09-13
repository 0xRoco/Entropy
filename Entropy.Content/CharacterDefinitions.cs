using System.Text.Json;

namespace Entropy.Content;

public sealed class CharacterCatalog
{
    public required List<ScenarioDefinition> Scenarios { get; set; }
    public required List<ProfessionDefinition> Professions { get; set; }
    public required List<BackgroundDefinition> Backgrounds { get; set; }
    public required List<TraitDefinition> Traits { get; set; }
    public required List<SkillDefinition> Skills { get; set; }

    public static CharacterCatalog Load(string path)
    {
        var catalog = JsonSerializer.Deserialize<CharacterCatalog>(File.ReadAllText(path));
        return catalog ?? throw new InvalidOperationException($"Character catalog '{path}' is empty.");
    }
}

public abstract class CharacterOption
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class ScenarioDefinition : CharacterOption
{
    public required string StartMapId { get; set; }
    public int StartX { get; set; }
    public int StartY { get; set; }
}

public sealed class ProfessionDefinition : CharacterOption
{
    public List<string> StartingItems { get; set; } = [];
    public List<string> Skills { get; set; } = [];
}

public sealed class BackgroundDefinition : CharacterOption
{
    public List<string> StartingItems { get; set; } = [];
    public List<string> Skills { get; set; } = [];
}

public sealed class TraitDefinition : CharacterOption
{
    public int Cost { get; set; }
}

public sealed class SkillDefinition : CharacterOption
{
    public string Attribute { get; set; } = "intelligence";
}

public sealed class CharacterBuild
{
    public string Name { get; set; } = "Alex";
    public string ScenarioId { get; set; } = string.Empty;
    public string ProfessionId { get; set; } = string.Empty;
    public string BackgroundId { get; set; } = string.Empty;
    public Dictionary<string, int> Stats { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["strength"] = 8,
        ["dexterity"] = 8,
        ["intelligence"] = 8,
        ["perception"] = 8,
        ["constitution"] = 8,
        ["will"] = 8
    };
    public List<string> TraitIds { get; set; } = [];
    public List<string> SkillIds { get; set; } = [];
}
