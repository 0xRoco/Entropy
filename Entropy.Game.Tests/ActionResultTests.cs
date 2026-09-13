using Entropy.Game.Systems;
using Xunit;

namespace Entropy.Game.Tests;

public class ActionResultTests
{
    [Fact]
    public void FailedActionDoesNotAdvanceTime()
    {
        Assert.False(ActionResult.Failed.AdvancesTime);
    }

    [Fact]
    public void FreeActionDoesNotAdvanceTime()
    {
        Assert.False(ActionResult.Free.AdvancesTime);
    }

    [Fact]
    public void TurnActionAdvancesTime()
    {
        var action = new ActionResult(true, true, 7);

        Assert.True(action.AdvancesTime);
        Assert.Equal(7, action.TimeCostMinutes);
    }

    [Fact]
    public void UnsuccessfulTurnActionDoesNotAdvanceTime()
    {
        var action = new ActionResult(false, true);

        Assert.False(action.AdvancesTime);
    }
}
