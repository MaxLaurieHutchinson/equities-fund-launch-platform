using FundLaunch.Platform.Core;

namespace FundLaunch.Platform.Core.Tests;

public sealed class AggregatorAllocatorTests
{
    [Fact]
    public void Aggregator_Builds_Deterministic_Signal_Set()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();

        var a = StrategyAggregator.Build(scenario.Signals);
        var b = StrategyAggregator.Build(scenario.Signals);

        Assert.Equal(a.Count, b.Count);
        Assert.Equal(a.Select(x => x.Symbol), b.Select(x => x.Symbol));
        Assert.Equal(a.Select(x => x.CompositeScore), b.Select(x => x.CompositeScore));
    }

    [Fact]
    public void Allocator_Respects_PerSymbol_And_Gross_Limits()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();
        var signals = StrategyAggregator.Build(scenario.Signals);

        var allocations = CapitalAllocator.Build(signals, scenario.CurrentBook, scenario.Limits);

        Assert.All(allocations, x => Assert.True(Math.Abs(x.TargetWeight) <= scenario.Limits.MaxAbsWeightPerSymbol + 0.000001m));
        Assert.True(allocations.Sum(x => Math.Abs(x.TargetWeight)) <= scenario.Limits.MaxGrossExposure + 0.000001m);
    }
}
