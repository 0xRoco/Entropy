using Entropy.Engine.Core;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;

namespace Entropy.Game.UI;

public class EnvironmentPanel : Panel
{
    private static readonly string[] MoonPhases =
    [
        "New", "Waxing crescent", "First quarter", "Waxing gibbous",
        "Full", "Waning gibbous", "Last quarter", "Waning crescent"
    ];

    private readonly GameContext _context;
    private readonly Label _placeLabel;
    private readonly Label _lightingLabel;
    private readonly Label _weatherLabel;
    private readonly Label _moonLabel;
    private readonly Label _dateLabel;
    private readonly Label _timeLabel;

    public EnvironmentPanel(GameContext context)
    {
        _context = context;
        Height = 9;

        Add(new Label { X = 1, Y = 1, Width = 22, Text = "ENVIRONMENT", Color = UiTheme.Keybind });

        _placeLabel = new Label { X = 1, Y = 2, Width = 22 };
        _lightingLabel = new Label { X = 1, Y = 3, Width = 22 };
        _weatherLabel = new Label { X = 1, Y = 4, Width = 22 };
        _moonLabel = new Label { X = 1, Y = 5, Width = 22 };
        _dateLabel = new Label { X = 1, Y = 6, Width = 22 };
        _timeLabel = new Label { X = 1, Y = 7, Width = 22 };

        Add(_placeLabel);
        Add(_lightingLabel);
        Add(_weatherLabel);
        Add(_moonLabel);
        Add(_dateLabel);
        Add(_timeLabel);
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var clock = _context.Clock;
        var hour = clock.TotalMinutes / 60 % 24;
        var lighting = hour switch
        {
            >= 21 or < 5 => "Dark",
            < 8 => "Dawn",
            < 18 => "Day",
            _ => "Dusk"
        };

        var day = clock.TotalMinutes / 1440;

        _placeLabel.Text = $"Place:   {_context.MapId}";
        _lightingLabel.Text = $"Lighting: {lighting}";
        _weatherLabel.Text = "Weather: Clear";
        _moonLabel.Text = $"Moon:    {MoonPhases[day * 8 / 28 % 8]}";
        _dateLabel.Text = $"Date:    {clock.Date()}";
        _timeLabel.Text = $"Time:    {clock.Time()}";

        base.Draw(context, offsetX, offsetY);
    }
}