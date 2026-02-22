using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public sealed class FundLaunchEngine
{
    public PlatformRunResult Run(FundLaunchScenario scenario)
    {
        scenario.Limits.Validate();

        var timestamp = DateTime.UtcNow;
        var runId = $"RUN-{timestamp:yyyyMMddHHmmssfff}";
        var registry = scenario.PluginRegistry ?? StrategyPluginRegistry.Empty;

        var pluginInit = registry.ExecuteInitialize(scenario.Signals, timestamp, runId);
        var lifecycleEvents = new List<StrategyPluginLifecycleEvent>(pluginInit.Events);

        var signals = StrategyAggregator.Build(pluginInit.Signals);
        lifecycleEvents.AddRange(registry.ExecuteCompositePublished(signals, timestamp, runId));

        var policy = PolicyOverrideEngine.Apply(scenario.Limits, scenario.PolicyOverrides, timestamp);

        IReadOnlyList<AllocationDraft> allocations;
        IReadOnlyList<StrategyBookAllocationSummary> strategyBooks;
        if (scenario.StrategyBooks is { Count: > 0 })
        {
            var multiBook = CapitalAllocator.BuildForStrategyBooks(pluginInit.Signals, scenario.StrategyBooks, policy.EffectiveLimits);
            allocations = multiBook.PortfolioAllocations;
            strategyBooks = multiBook.BookSummaries;
        }
        else
        {
            allocations = CapitalAllocator.Build(signals, scenario.CurrentBook, policy.EffectiveLimits);
            strategyBooks = Array.Empty<StrategyBookAllocationSummary>();
        }

        var risk = RiskGate.Evaluate(allocations, policy.EffectiveLimits);
        var intents = ExecutionPlanner.Build(allocations, risk, policy.EffectiveLimits);
        var incident = IncidentSimulator.Run(signals, intents, scenario.IncidentSimulation, timestamp);
        var telemetry = TelemetryBuilder.Build(allocations, risk, incident.AdjustedIntents, incident);

        var run = new PlatformRunResult(
            Timestamp: timestamp,
            Signals: signals,
            Allocations: allocations,
            Risk: risk,
            ExecutionIntents: incident.AdjustedIntents,
            Telemetry: telemetry,
            StrategyBooks: strategyBooks,
            PolicyAudit: policy.AuditTrail,
            StrategyLifecycle: lifecycleEvents,
            IncidentSimulation: incident);

        var completionEvents = registry.ExecuteRunCompleted(run, timestamp, runId);
        return run with
        {
            StrategyLifecycle = run.StrategyLifecycle
                .Concat(completionEvents)
                .OrderBy(x => x.StrategyId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Hook, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    public static PlatformRunSummary BuildSummary(PlatformRunResult run)
    {
        var topSignal = run.Signals
            .OrderByDescending(x => Math.Abs(x.CompositeScore))
            .ThenBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var appliedPolicyCount = run.PolicyAudit.Count(x => string.Equals(x.Status, "APPLIED", StringComparison.OrdinalIgnoreCase));
        var pendingPolicyCount = run.PolicyAudit.Count(x => string.Equals(x.Status, "PENDING_APPROVAL", StringComparison.OrdinalIgnoreCase));

        return new PlatformRunSummary(
            SignalSymbolCount: run.Signals.Count,
            AllocationCount: run.Allocations.Count,
            StrategyBookCount: run.StrategyBooks.Count,
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
            ControlState: run.Telemetry.ControlState,
            AppliedPolicyOverrideCount: appliedPolicyCount,
            PendingPolicyOverrideCount: pendingPolicyCount,
            StrategyLifecycleEvents: run.StrategyLifecycle.Count,
            IncidentTimelineEvents: run.IncidentSimulation.Timeline.Count,
            IncidentReplayFrames: run.IncidentSimulation.ReplayFrames.Count,
            ActiveIncidentFaults: run.IncidentSimulation.ActiveFaults.Count);
    }
}
