namespace Entropy.Game.Components.AI;

public enum ScheduleActivity { Wander, Home, Work, Sleep }

public record ScheduleEntry(
    DayOfWeek? Day,       // null = every day
    int StartMinute,      // minutes since midnight
    int EndMinute,        // exclusive: Start > End wraps past midnight
    ScheduleActivity Activity);

public struct Schedule
{
    public List<ScheduleEntry> Entries;

    public static Schedule Create() => new() { Entries = new List<ScheduleEntry>() };

    public ScheduleActivity ActivityAt(int minuteOfDay, DayOfWeek day)
    {
        foreach (var entry in Entries)
        {
            if (entry.Day is { } d && d != day) continue;

            var inWindow = entry.StartMinute <= entry.EndMinute
                ? minuteOfDay >= entry.StartMinute && minuteOfDay < entry.EndMinute
                : minuteOfDay >= entry.StartMinute || minuteOfDay < entry.EndMinute;

            if (inWindow) return entry.Activity;
        }
        return ScheduleActivity.Wander;
    }
}
