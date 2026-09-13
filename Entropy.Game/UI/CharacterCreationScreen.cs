using Entropy.Content;
using Entropy.Engine.UI;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public sealed class CharacterCreationScreen
{
    public event Action<CharacterBuild>? Confirmed;
    public event Action? Cancelled;

    private static readonly string[] Tabs = ["IDENTITY", "SCENARIO", "PROFESSION", "BACKGROUND", "STATS", "TRAITS", "SKILLS", "SUMMARY"];
    private static readonly string[] StatNames = ["Strength", "Dexterity", "Intelligence", "Perception", "Constitution", "Will"];
    private static readonly string[] Names = ["Alex", "Dana", "Morgan", "Riley", "Sam", "Taylor"];

    private readonly CharacterCatalog _catalog;
    private readonly CharacterBuild _build;
    private readonly int[] _selection;
    private Vector2i _viewport;
    private int _tab;
    private int _points = 10;
    private int _statPoints = 6;

    public CharacterCreationScreen(CharacterCatalog catalog, Vector2i viewportTiles)
    {
        _catalog = catalog;
        _viewport = viewportTiles;
        _build = new CharacterBuild
        {
            Name = Names[Random.Shared.Next(Names.Length)],
            ScenarioId = catalog.Scenarios[0].Id,
            ProfessionId = catalog.Professions[0].Id,
            BackgroundId = catalog.Backgrounds[0].Id
        };
        _selection = new int[7];
    }

    public bool HandleKey(Keys key)
    {
        switch (key)
        {
            case Keys.Escape:
                Cancelled?.Invoke();
                return true;
            case Keys.Left:
                _tab = (_tab + Tabs.Length - 1) % Tabs.Length;
                return true;
            case Keys.Right or Keys.Tab:
                _tab = (_tab + 1) % Tabs.Length;
                return true;
            case Keys.Up:
                MoveSelection(-1);
                return true;
            case Keys.Down:
                MoveSelection(1);
                return true;
            case Keys.Delete:
                DecreaseStat();
                return true;
            case Keys.Enter:
                ActivateSelection();
                return true;
            case Keys.R:
                Randomize();
                return true;
            case Keys.N:
                _build.Name = Names[Random.Shared.Next(Names.Length)];
                return true;
            default:
                return false;
        }
    }

    public void Draw(DrawContext context)
    {
        context.DrawRect(0, 0, _viewport.X, _viewport.Y, Color4.Black);

        context.DrawText(2, 1, "ENTROPY CHARACTER CREATION", UiTheme.Valid);
        context.DrawText(2, 3, $"Name: {_build.Name}    Trait points: {_points}/10", UiTheme.TextBright);
        context.DrawText(2, 5, "[LEFT/RIGHT] tab  [UP/DOWN] select  [ENTER] choose  [R] randomize  [ESC] cancel", UiTheme.TextDim);

        var x = 2;
        for (var i = 0; i < Tabs.Length; i++)
        {
            var color = i == _tab ? UiTheme.TextBright : UiTheme.TextDim;
            context.DrawText(x, 7, i == _tab ? $">{Tabs[i]}<" : Tabs[i], color);
            x += Tabs[i].Length + 4;
        }

        if (_tab == Tabs.Length - 1)
            DrawSummary(context);
        else
            DrawOptions(context);
    }

    public void Resize(Vector2i viewportTiles)
    {
        _viewport = viewportTiles;
    }

    private void MoveSelection(int delta)
    {
        var count = OptionCount();
        if (count == 0) return;
        _selection[_tab] = Math.Clamp(_selection[_tab] + delta, 0, count - 1);
    }

    private void ActivateSelection()
    {
        if (_tab == Tabs.Length - 1)
        {
            Confirmed?.Invoke(_build);
            return;
        }

        var index = _selection[_tab];
        switch (_tab)
        {
            case 0:
                _build.Name = Names[index % Names.Length];
                break;
            case 1:
                _build.ScenarioId = _catalog.Scenarios[index].Id;
                break;
            case 2:
                _build.ProfessionId = _catalog.Professions[index].Id;
                break;
            case 3:
                _build.BackgroundId = _catalog.Backgrounds[index].Id;
                break;
            case 4:
                IncreaseStat();
                break;
            case 5:
                ToggleTrait(index);
                break;
            case 6:
                ToggleSkill(index);
                break;
        }
    }

    private void ToggleTrait(int index)
    {
        var id = _catalog.Traits[index].Id;
        if (_build.TraitIds.Remove(id))
        {
            _points += _catalog.Traits[index].Cost;
            return;
        }

        if (_points - _catalog.Traits[index].Cost < 0) return;
        _build.TraitIds.Add(id);
        _points -= _catalog.Traits[index].Cost;
    }

    private void ToggleSkill(int index)
    {
        var id = _catalog.Skills[index].Id;
        if (_build.SkillIds.Contains(id))
            _build.SkillIds.Remove(id);
        else if (_build.SkillIds.Count < 5)
            _build.SkillIds.Add(id);
    }

    private void Randomize()
    {
        _build.Name = Names[Random.Shared.Next(Names.Length)];
        _build.ScenarioId = _catalog.Scenarios[Random.Shared.Next(_catalog.Scenarios.Count)].Id;
        _build.ProfessionId = _catalog.Professions[Random.Shared.Next(_catalog.Professions.Count)].Id;
        _build.BackgroundId = _catalog.Backgrounds[Random.Shared.Next(_catalog.Backgrounds.Count)].Id;
        _build.TraitIds.Clear();
        _build.SkillIds.Clear();
        _points = 10;
        _statPoints = 6;
        foreach (var stat in StatNames)
            _build.Stats[stat.ToLowerInvariant()] = 8;
    }

    private int OptionCount() => _tab switch
    {
        0 => Names.Length,
        1 => _catalog.Scenarios.Count,
        2 => _catalog.Professions.Count,
        3 => _catalog.Backgrounds.Count,
        4 => StatNames.Length,
        5 => _catalog.Traits.Count,
        6 => _catalog.Skills.Count,
        _ => 0
    };

    private void DrawOptions(DrawContext context)
    {
        if (_tab == 4)
        {
            DrawStats(context);
            return;
        }

        var options = _tab switch
        {
            0 => Names.Select(name => new NamedOption(name, "Choose the name you want to use in the neighborhood.")).Cast<CharacterOption>().ToList(),
            1 => _catalog.Scenarios.Cast<CharacterOption>().ToList(),
            2 => _catalog.Professions.Cast<CharacterOption>().ToList(),
            3 => _catalog.Backgrounds.Cast<CharacterOption>().ToList(),
            5 => _catalog.Traits.Cast<CharacterOption>().ToList(),
            _ => _catalog.Skills.Cast<CharacterOption>().ToList()
        };
        var start = Math.Max(0, _selection[_tab] - 12);
        var end = Math.Min(options.Count, start + 18);

        for (var i = start; i < end; i++)
        {
            var option = options[i];
            var selected = i == _selection[_tab];
            var chosen = IsChosen(i);
            var marker = selected ? ">" : chosen ? "+" : " ";
            var suffix = _tab == 5 ? $" [{_catalog.Traits[i].Cost:+#;-#;0}]" : "";
            context.DrawText(3, 9 + i - start, $"{marker} {option.Name}{suffix}", selected ? UiTheme.TextBright : chosen ? UiTheme.Valid : UiTheme.Text);
        }

        var selectedOption = options[_selection[_tab]];
        context.DrawText(42, 10, selectedOption.Name, UiTheme.Info);
        DrawWrapped(context, selectedOption.Description, 42, 12, 36);
    }

    private void DrawSummary(DrawContext context)
    {
        var scenario = _catalog.Scenarios.Single(option => option.Id == _build.ScenarioId);
        var profession = _catalog.Professions.Single(option => option.Id == _build.ProfessionId);
        var background = _catalog.Backgrounds.Single(option => option.Id == _build.BackgroundId);

        context.DrawText(3, 10, "CHARACTER SUMMARY", UiTheme.Info);
        context.DrawText(3, 12, $"Name: {_build.Name}", UiTheme.TextBright);
        context.DrawText(3, 13, $"Scenario: {scenario.Name}", UiTheme.Text);
        context.DrawText(3, 14, $"Profession: {profession.Name}", UiTheme.Text);
        context.DrawText(3, 15, $"Background: {background.Name}", UiTheme.Text);
        context.DrawText(3, 17, $"Stats: {string.Join(", ", _build.Stats.Select(pair => $"{pair.Key} {pair.Value}"))}", UiTheme.Text);
        context.DrawText(3, 18, $"Traits: {(_build.TraitIds.Count == 0 ? "None" : string.Join(", ", _build.TraitIds))}", UiTheme.Text);
        context.DrawText(3, 19, $"Skills: {(_build.SkillIds.Count == 0 ? "None" : string.Join(", ", _build.SkillIds))}", UiTheme.Text);
        context.DrawText(3, 21, "Press ENTER to begin with this character.", UiTheme.Valid);
    }

    private bool IsChosen(int index) => _tab switch
    {
        5 => _build.TraitIds.Contains(_catalog.Traits[index].Id),
        6 => _build.SkillIds.Contains(_catalog.Skills[index].Id),
        _ => false
    };

    private void IncreaseStat()
    {
        if (_statPoints <= 0) return;
        var key = StatNames[_selection[4]].ToLowerInvariant();
        _build.Stats[key]++;
        _statPoints--;
    }

    private void DecreaseStat()
    {
        if (_tab != 4) return;
        var key = StatNames[_selection[4]].ToLowerInvariant();
        if (_build.Stats[key] <= 8) return;
        _build.Stats[key]--;
        _statPoints++;
    }

    private void DrawStats(DrawContext context)
    {
        context.DrawText(42, 10, $"STAT POINTS REMAINING: {_statPoints}", UiTheme.Info);
        for (var i = 0; i < StatNames.Length; i++)
        {
            var key = StatNames[i].ToLowerInvariant();
            var selected = i == _selection[4];
            context.DrawText(3, 9 + i, $"{(selected ? ">" : " ")} {StatNames[i],-14} {_build.Stats[key]}", selected ? UiTheme.TextBright : UiTheme.Text);
        }
        context.DrawText(42, 12, "ENTER raises the selected stat.", UiTheme.TextDim);
        context.DrawText(42, 13, "DELETE lowers it back toward 8.", UiTheme.TextDim);
    }

    private sealed class NamedOption : CharacterOption
    {
        [SetsRequiredMembers]
        public NamedOption(string name, string description)
        {
            Id = name;
            Name = name;
            Description = description;
        }
    }

    private static void DrawWrapped(DrawContext context, string text, int x, int y, int width)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = "";
        var row = 0;
        foreach (var word in words)
        {
            if (line.Length + word.Length + 1 > width)
            {
                context.DrawText(x, y + row++, line, UiTheme.TextDim);
                line = "";
            }
            line = line.Length == 0 ? word : $"{line} {word}";
        }
        if (line.Length > 0)
            context.DrawText(x, y + row, line, UiTheme.TextDim);
    }
}
