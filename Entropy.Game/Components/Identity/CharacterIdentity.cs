namespace Entropy.Game.Components.Identity;

public struct CharacterIdentity
{
    public string Name;
    public string ProfessionId;
    public string BackgroundId;
    public Dictionary<string, int> Stats;
    public List<string> TraitIds;
    public List<string> SkillIds;

    public static CharacterIdentity Create() => new()
    {
        Stats = new(StringComparer.OrdinalIgnoreCase),
        TraitIds = [],
        SkillIds = []
    };
}
