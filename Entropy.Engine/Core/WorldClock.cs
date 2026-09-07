using System.Globalization;

namespace Entropy.Engine.Core;

public enum Season { Spring, Summer, Autumn, Winter }

public sealed class WorldClock(int year, int month, int day, int hour, int minute)
{
    public int TotalMinutes { get; private set; }
    private DateTime _time = new(year, month, day, hour, minute, 0);
    
    public void Advance(int minutes)
    {
        TotalMinutes += minutes;
        _time = _time.AddMinutes(minutes);
    }
    
    public DayOfWeek Weekday => _time.DayOfWeek;
    public int Year => _time.Year;
    public int Month => _time.Month;
    public int Day => _time.Day;
    public int Hour => _time.Hour;
    public int Minute => _time.Minute;

    public int MinuteOfDay => _time.Hour * 60 + _time.Minute;

    public Season Season => _time.Month switch
    {
        >= 3 and <= 5 => Season.Spring,
        >= 6 and <= 8 => Season.Summer,
        >= 9 and <= 11 => Season.Autumn,
        _ => Season.Winter
    };

    public string Time() => _time.ToString("HH:mm", CultureInfo.InvariantCulture);
    public string Date() => _time.ToString("ddd MMM dd yyyy", CultureInfo.InvariantCulture);
}