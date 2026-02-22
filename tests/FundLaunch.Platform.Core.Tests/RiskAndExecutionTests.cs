using FundLaunch.Platform.Contracts;
using FundLaunch.Platform.Core;

namespace FundLaunch.Platform.Core.Tests;

public sealed class RiskAndExecutionTests
{
    [Fact]
    public void RiskGate_Rejects_When_Turnover_Exceeded()
    {
        var limits = new RiskLimitConfig(0.30m, 1.0m, 0.20m, 0.50m, 0m, 1_000_000m);
        var allocations = new[]
        {
            new AllocationDraft("AAPL", 0m, 0.25m, 0.25m, "BUY", "test"),
            new AllocationDraft("MSFT", 0m, -0.25m, -0.25m, "SELL", "test")
        };

        var decision = RiskGate.Evaluate(allocations, limits);

        Assert.False(decision.Approved);
        Assert.Contains(decision.Breaches, x => x.StartsWith("Turnover:", StringComparison.Ordinal));
    }

    [Fact]
    public void ExecutionPlanner_Skips_When_Risk_Not_Approved()
    {
        var limits = new RiskLimitConfig(0.30m, 1.0m, 0.80m, 0.50m, 10000m, 2_000_000m);
        var allocations = new[]
        {
            new AllocationDraft("AAPL", 0.10m, 0.18m, 0.08m, "BUY", "test")
        };

        var risk = new RiskDecision(false, "REJECTED", "test", 0m, 0m, 0m, new[] { "breach" });
        var intents = ExecutionPlanner.Build(allocations, risk, limits);

        Assert.Empty(intents);
    }
}
