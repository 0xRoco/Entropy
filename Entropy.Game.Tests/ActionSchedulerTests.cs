using Entropy.Engine.Core;
using Entropy.Game.Systems;
using Xunit;

namespace Entropy.Game.Tests;

public class ActionSchedulerTests
{
    [Fact]
    public void ConsumingActionAdvancesClockByItsCost()
    {
        var clock = new WorldClock(2001, 3, 12, 7, 30);

        var processed = ActionScheduler.Process(new ActionResult(true, true, 15), clock.Advance);

        Assert.True(processed);
        Assert.Equal(15, clock.TotalMinutes);
        Assert.Equal("07:45", clock.Time());
    }

    [Fact]
    public void FailedActionDoesNotInvokeAdvance()
    {
        var advanceCalls = 0;

        var processed = ActionScheduler.Process(ActionResult.Failed, _ => advanceCalls++);

        Assert.False(processed);
        Assert.Equal(0, advanceCalls);
    }

    [Fact]
    public void FreeActionDoesNotInvokeAdvance()
    {
        var advanceCalls = 0;

        var processed = ActionScheduler.Process(ActionResult.Free, _ => advanceCalls++);

        Assert.False(processed);
        Assert.Equal(0, advanceCalls);
    }
}
