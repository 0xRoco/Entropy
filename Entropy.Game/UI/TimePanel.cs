using Entropy.Engine.Core;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;

namespace Entropy.Game.UI;

public class TimePanel : Panel
{
    private readonly Label _dateLabel;
    private readonly Label _timeLabel;
    private readonly WorldClock _clock;

    public TimePanel(WorldClock clock)
    {
        _clock = clock;
        Height = 5;

        const int innerX = 1;
        Add(new Label { X = innerX, Y = 1, Width = 22, Text = "TIME", Color = UiTheme.Keybind });
        _dateLabel = new Label { X = innerX, Y = 2, Width = 22 };
        _timeLabel = new Label { X = innerX, Y = 3, Width = 22 };
        Add(_dateLabel);
        Add(_timeLabel);
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        _dateLabel.Text = _clock.Date();
        _timeLabel.Text = _clock.Time() + $"  ({_clock.Season})";
        base.Draw(context, offsetX, offsetY);
    }
}