using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public sealed class FundLaunchEngine
{
    public PlatformRunResult Run(FundLaunchScenario scenario)
    {
        scenario.Limits.Validate();

        var timestamp = ResolveTimestamp(scenario.FixedTimestampUtc);
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
        var baselineIntents = ExecutionPlanner.Build(allocations, risk, policy.EffectiveLimits);

        var eventBus = new InMemoryRuntimeEventBus();
        var incident = IncidentSimulator.Run(signals, baselineIntents, scenario.IncidentSimulation, timestamp, eventBus);
        var tca = TcaAnalyzer.Analyze(baselineIntents, incident.AdjustedIntents, incident, timestamp, eventBus);
        var feedback = FeedbackLoopEngine.BuildRecommendations(tca, risk, incident, timestamp, eventBus);
        var arena = AgentArenaEngine.Run(
            strategyBooks,
            tca,
            feedback,
            incident,
            risk,
            scenario.AgentArena,
            timestamp,
            eventBus);
        var incidentWithTimeline = incident with { Timeline = eventBus.Snapshot() };

        var telemetry = TelemetryBuilder.Build(
            allocations,
            risk,
            incidentWithTimeline.AdjustedIntents,
            incidentWithTimeline,
            tca,
            feedback);

        var run = new PlatformRunResult(
            Timestamp: timestamp,
            Signals: signals,
            Allocations: allocations,
            Risk: risk,
            ExecutionIntents: incidentWithTimeline.AdjustedIntents,
            Telemetry: telemetry,
            StrategyBooks: strategyBooks,
            PolicyAudit: policy.AuditTrail,
            StrategyLifecycle: lifecycleEvents,
            IncidentSimulation: incidentWithTimeline,
            TcaAnalysis: tca,
            FeedbackLoop: feedback,
            AgentArena: arena);

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
            ActiveIncidentFaults: run.IncidentSimulation.ActiveFaults.Count,
            TcaAvgFillRate: run.TcaAnalysis.Summary.AvgFillRate,
            TcaAvgSlippageBps: run.TcaAnalysis.Summary.AvgSlippageBps,
            TcaTotalEstimatedCost: run.TcaAnalysis.Summary.TotalEstimatedCost,
            FeedbackRecommendationCount: run.FeedbackLoop.Summary.RecommendationCount,
            FeedbackApprovedCount: run.FeedbackLoop.Summary.ApprovedCount,
            FeedbackBlockedCount: run.FeedbackLoop.Summary.BlockedCount,
            FeedbackPolicyState: run.FeedbackLoop.Summary.PolicyState,
            AgentArenaRounds: run.AgentArena.Summary.RoundsExecuted,
            AgentArenaAgents: run.AgentArena.Summary.ParticipatingAgents,
            AgentArenaConvergenceScore: run.AgentArena.Summary.ConvergenceScore,
            AgentArenaPolicyState: run.AgentArena.Summary.PolicyState);
    }

    private static DateTime ResolveTimestamp(DateTime? maybeTimestamp)
    {
        if (!maybeTimestamp.HasValue)
        {
            return DateTime.UtcNow;
        }

        var timestamp = maybeTimestamp.Value;
        if (timestamp.Kind == DateTimeKind.Utc)
        {
            return timestamp;
        }

        if (timestamp.Kind == DateTimeKind.Local)
        {
            return timestamp.ToUniversalTime();
        }

        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }
}
