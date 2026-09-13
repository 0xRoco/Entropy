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
        context.DrawRect(0, 0, _viewport.X, _viewport.Y, UiTheme.PanelBackground);
        context.DrawText(2, 1, "ENTROPY // CHARACTER CREATION", UiTheme.Info);
        context.DrawText(2, 2, $"Name: {_build.Name}", UiTheme.TextBright);
        context.DrawText(Math.Max(30, _viewport.X - 24), 2, $"Traits: {_points,2}/10", UiTheme.Keybind);

        var x = 2;
        for (var i = 0; i < Tabs.Length; i++)
        {
            var label = i == _tab ? $"[{Tabs[i]}]" : $" {Tabs[i]} ";
            context.DrawText(x, 4, label, i == _tab ? UiTheme.TextBright : UiTheme.TextDim);
            x += label.Length + 2;
        }
        TerminalChrome.Divider(context, 1, 5, Math.Max(1, _viewport.X - 2));

        if (_tab == Tabs.Length - 1)
            DrawSummary(context, new UiRect(2, 7, Math.Max(1, _viewport.X - 4), Math.Max(1, _viewport.Y - 11)));
        else
            DrawOptions(context, new UiRect(2, 7, Math.Max(1, _viewport.X - 4), Math.Max(1, _viewport.Y - 11)));

        TerminalChrome.Footer(context, 1, Math.Max(6, _viewport.Y - 3), Math.Max(1, _viewport.X - 2),
            ('<', "prev tab"), ('>', "next tab"), ('j', "select"), ('e', "choose"), ('r', "randomize"), ('?', "help"));
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

    private void DrawOptions(DrawContext context, UiRect area)
    {
        if (_tab == 4)
        {
            DrawStats(context, area);
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
        var listWidth = Math.Min(36, Math.Max(24, area.Width / 3));
        var detailX = area.X + listWidth + 2;
        var detailWidth = Math.Max(1, area.Width - listWidth - 2);
        TerminalChrome.Window(context, new UiRect(area.X, area.Y, listWidth, area.Height), Tabs[_tab]);
        TerminalChrome.Window(context, new UiRect(detailX, area.Y, detailWidth, area.Height), "Selection");

        var visibleRows = Math.Max(1, Math.Min(18, area.Height - 4));
        var start = Math.Max(0, Math.Min(_selection[_tab] - visibleRows / 2, options.Count - visibleRows));
        var end = Math.Min(options.Count, start + visibleRows);

        context.PushClip(new UiRect(area.X + 1, area.Y + 1, listWidth - 2, area.Height - 2));
        for (var i = start; i < end; i++)
        {
            var option = options[i];
            var selected = i == _selection[_tab];
            var chosen = IsChosen(i);
            var marker = chosen ? "+" : " ";
            var suffix = _tab == 5 ? $" {_catalog.Traits[i].Cost:+#;-#;0}" : "";
            var text = $"{marker} {option.Name}{suffix}";
            TerminalChrome.SelectionRow(context, area.X + 1, area.Y + 2 + i - start, listWidth - 2, text,
                selected, chosen ? UiTheme.Valid : UiTheme.Text);
        }
        context.PopClip();

        var selectedOption = options[_selection[_tab]];
        context.PushClip(new UiRect(detailX + 1, area.Y + 1, detailWidth - 2, area.Height - 2));
        context.DrawText(detailX + 2, area.Y + 2, selectedOption.Name, UiTheme.Info);
        DrawWrapped(context, selectedOption.Description, detailX + 2, area.Y + 4, Math.Max(1, detailWidth - 4));
        if (_tab == 5)
            context.DrawText(detailX + 2, area.Bottom - 2, IsChosen(_selection[_tab]) ? "ENTER removes trait" : "ENTER adds trait", UiTheme.Keybind);
        else if (_tab == 6)
            context.DrawText(detailX + 2, area.Bottom - 2, $"Selected skills: {_build.SkillIds.Count}/5", UiTheme.Keybind);
        context.PopClip();
    }

    private void DrawSummary(DrawContext context, UiRect area)
    {
        var scenario = _catalog.Scenarios.Single(option => option.Id == _build.ScenarioId);
        var profession = _catalog.Professions.Single(option => option.Id == _build.ProfessionId);
        var background = _catalog.Backgrounds.Single(option => option.Id == _build.BackgroundId);

        var leftWidth = Math.Max(25, area.Width / 2);
        TerminalChrome.Window(context, new UiRect(area.X, area.Y, leftWidth, area.Height), "Character");
        TerminalChrome.Window(context, new UiRect(area.X + leftWidth + 2, area.Y, area.Width - leftWidth - 2, area.Height), "Loadout");
        context.DrawText(area.X + 2, area.Y + 2, $"Name:       {_build.Name}", UiTheme.TextBright);
        context.DrawText(area.X + 2, area.Y + 3, $"Scenario:   {scenario.Name}", UiTheme.Text);
        context.DrawText(area.X + 2, area.Y + 4, $"Profession: {profession.Name}", UiTheme.Text);
        context.DrawText(area.X + 2, area.Y + 5, $"Background: {background.Name}", UiTheme.Text);
        context.DrawText(area.X + 2, area.Y + 7, "ATTRIBUTES", UiTheme.Heading);
        var row = area.Y + 8;
        foreach (var pair in _build.Stats)
            context.DrawText(area.X + 2, row++, $"{pair.Key,-14} {pair.Value,2}", UiTheme.Text);
        context.DrawText(area.X + leftWidth + 4, area.Y + 2, "TRAITS", UiTheme.Heading);
        DrawWrapped(context, _build.TraitIds.Count == 0 ? "None" : string.Join(", ", _build.TraitIds), area.X + leftWidth + 4, area.Y + 3, area.Width - leftWidth - 6);
        context.DrawText(area.X + leftWidth + 4, area.Y + 7, "SKILLS", UiTheme.Heading);
        DrawWrapped(context, _build.SkillIds.Count == 0 ? "None" : string.Join(", ", _build.SkillIds), area.X + leftWidth + 4, area.Y + 8, area.Width - leftWidth - 6);
        context.DrawText(area.X + 2, area.Bottom - 3, "ENTER begins the run", UiTheme.Valid);
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

    private void DrawStats(DrawContext context, UiRect area)
    {
        var panelWidth = Math.Min(44, area.Width);
        TerminalChrome.Window(context, new UiRect(area.X, area.Y, panelWidth, area.Height), "Attributes");
        context.DrawText(area.X + 2, area.Y + 2, $"POINTS REMAINING: {_statPoints}", UiTheme.Keybind);
        for (var i = 0; i < StatNames.Length; i++)
        {
            var key = StatNames[i].ToLowerInvariant();
            var selected = i == _selection[4];
            TerminalChrome.SelectionRow(context, area.X + 1, area.Y + 4 + i, panelWidth - 2,
                $"{StatNames[i],-14} {_build.Stats[key],2}", selected);
        }
        if (area.Width > panelWidth + 4)
        {
            context.DrawText(area.X + panelWidth + 3, area.Y + 2, "HOW ATTRIBUTES WORK", UiTheme.Heading);
            DrawWrapped(context, "Attributes shape what you can carry, notice, endure, and survive. Build a person, not a perfect character.", area.X + panelWidth + 3, area.Y + 4, area.Width - panelWidth - 5);
        }
        context.DrawText(area.X + 2, area.Bottom - 2, "ENTER +1    DELETE -1", UiTheme.TextDim);
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
