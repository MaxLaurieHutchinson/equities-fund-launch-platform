using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public sealed class FundLaunchEngine
{
    public PlatformRunResult Run(FundLaunchScenario scenario)
    {
        scenario.Limits.Validate();

        var signals = StrategyAggregator.Build(scenario.Signals);
        var allocations = CapitalAllocator.Build(signals, scenario.CurrentBook, scenario.Limits);
        var risk = RiskGate.Evaluate(allocations, scenario.Limits);
        var intents = ExecutionPlanner.Build(allocations, risk, scenario.Limits);
        var telemetry = TelemetryBuilder.Build(allocations, risk, intents);

        return new PlatformRunResult(
            Timestamp: DateTime.UtcNow,
            Signals: signals,
            Allocations: allocations,
            Risk: risk,
            ExecutionIntents: intents,
            Telemetry: telemetry);
    }

    public static PlatformRunSummary BuildSummary(PlatformRunResult run)
    {
        var topSignal = run.Signals
            .OrderByDescending(x => Math.Abs(x.CompositeScore))
            .ThenBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return new PlatformRunSummary(
            SignalSymbolCount: run.Signals.Count,
            AllocationCount: run.Allocations.Count,
            RiskApproved: run.Risk.Approved,
            BreachCount: run.Risk.Breaches.Count,
            ExecutionIntentCount: run.ExecutionIntents.Count,
            GrossExposure: run.Risk.GrossExposure,
            NetExposure: run.Risk.NetExposure,
            Turnover: run.Risk.Turnover,
            TotalExecutionNotional: run.ExecutionIntents.Sum(x => x.Notional),
            TopSignalSymbol: topSignal?.Symbol ?? "(none)",
            TopSignalScore: topSignal?.CompositeScore ?? 0m,
            FleetHealthScore: run.Telemetry.FleetHealthScore,
            ControlState: run.Telemetry.ControlState);
    }
}
