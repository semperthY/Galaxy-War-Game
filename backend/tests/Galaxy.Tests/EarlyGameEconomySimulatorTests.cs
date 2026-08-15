using Galaxy.Application.Economy;

namespace Galaxy.Tests;

public class EarlyGameEconomySimulatorTests
{
    [Fact]
    public void First72Hours_HaveNoEconomicDeadEnd()
    {
        var result = EarlyGameEconomySimulator.Simulate();

        Assert.Equal(18, result.Actions.Count);
        Assert.True(
            result.MaxBlockedDuration <= TimeSpan.FromHours(2),
            $"Longest resource-only block was " +
            $"{result.MaxBlockedDuration}.");
        Assert.All(result.Actions, action =>
        {
            Assert.True(action.MaterialsAfter >= 0m);
            Assert.True(action.DeuteriumAfter >= 0m);
        });
    }
}
