namespace FundLaunch.Platform.Contracts;

public sealed record StrategySignal(
    string StrategyId,
    string Symbol,
    decimal AlphaScore,
    decimal Confidence);

public sealed record CompositeSignal(
    string Symbol,
    decimal CompositeScore,
    IReadOnlyList<string> Contributors);

public sealed record CurrentBookWeight(string Symbol, decimal Weight);

public sealed record AllocationDraft(
    string Symbol,
    decimal CurrentWeight,
    decimal TargetWeight,
    decimal DeltaWeight,
    string Action,
    string Rationale,
    string StrategyBookId = "MASTER");

public sealed record StrategyBookConfig(
    string BookId,
    IReadOnlyList<string> StrategyIds,
    decimal CapitalShare,
    IReadOnlyList<CurrentBookWeight> CurrentBook);

public sealed record StrategyBookAllocationSummary(
    string BookId,
    decimal CapitalShare,
    int AllocationCount,
    decimal GrossExposure,
    decimal NetExposure,
    decimal Turnover);

public sealed record RiskLimitConfig(
    decimal MaxAbsWeightPerSymbol,
    decimal MaxGrossExposure,
    decimal MaxTurnover,
    decimal MaxAbsNetExposure,
    decimal MinOrderNotional,
    decimal CapitalBase)
{
    public void Validate()
    {
        if (MaxAbsWeightPerSymbol <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxAbsWeightPerSymbol));
        }

        if (MaxGrossExposure <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxGrossExposure));
        }

        if (MaxTurnover <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxTurnover));
        }

        if (MaxAbsNetExposure < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxAbsNetExposure));
        }

        if (MinOrderNotional < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(MinOrderNotional));
        }

        if (CapitalBase <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(CapitalBase));
        }
    }
}

public sealed record RiskDecision(
    bool Approved,
    string Code,
    string Detail,
    decimal GrossExposure,
    decimal NetExposure,
    decimal Turnover,
    IReadOnlyList<string> Breaches);

public sealed record PolicyOverrideRequest(
    string PolicyKey,
    decimal Value,
    string Reason,
    string RequestedBy,
    DateTime RequestedAtUtc,
    string? ApprovedBy = null,
    DateTime? ApprovedAtUtc = null,
    DateTime? ExpiresAtUtc = null)
{
    public bool IsApproved => !string.IsNullOrWhiteSpace(ApprovedBy) && ApprovedAtUtc.HasValue;
}

public sealed record PolicyOverrideAuditEntry(
    string PolicyKey,
    decimal RequestedValue,
    decimal? PriorValue,
    decimal? AppliedValue,
    string Status,
    string Reason,
    string RequestedBy,
    string? ApprovedBy,
    DateTime RequestedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime EvaluatedAtUtc);

public sealed record ExecutionIntent(
    string Symbol,
    string Side,
    decimal DeltaWeight,
    decimal Notional,
    string Route,
    string Urgency,
    string StrategyBookId = "MASTER");

public sealed record StrategyPluginLifecycleEvent(
    string StrategyId,
    string Hook,
    string Status,
    string Detail,
    DateTime Timestamp);

public sealed record IncidentSimulationConfig(
    bool EnableLatencySpike,
    bool EnableVenueRejectBurst,
    bool EnableFeedDropout,
    decimal LatencySpikeMultiplier,
    decimal VenueRejectRatio,
    decimal FeedDropoutRatio);

public sealed record MarketRegimeSnapshot(
    string Regime,
    decimal VolatilityMultiplier,
    decimal LiquidityMultiplier,
    decimal SpreadBps);

public sealed record RuntimeEvent(
    int Sequence,
    DateTime Timestamp,
    string EventType,
    string Source,
    string Detail,
    decimal ImpactScore);

public sealed record IncidentReplayFrame(
    int Step,
    string Symbol,
    decimal BaselineNotional,
    decimal AdjustedNotional,
    string BaselineRoute,
    string AdjustedRoute,
    string Outcome);

public sealed record IncidentSimulationResult(
    MarketRegimeSnapshot Regime,
    IReadOnlyList<RuntimeEvent> Timeline,
    IReadOnlyList<string> ActiveFaults,
    IReadOnlyList<ExecutionIntent> AdjustedIntents,
    IReadOnlyList<IncidentReplayFrame> ReplayFrames,
    decimal RejectedNotional,
    decimal AddedLatencyMs);

public sealed record TcaFillMetric(
    string Symbol,
    string StrategyBookId,
    string Route,
    decimal IntendedNotional,
    decimal ExecutedNotional,
    decimal FillRate,
    decimal SlippageBps,
    decimal EstimatedCost,
    string QualityBand);

public sealed record TcaRouteSummary(
    string Route,
    int IntentCount,
    decimal AvgFillRate,
    decimal AvgSlippageBps,
    decimal TotalEstimatedCost,
    int PoorQualityCount);

public sealed record TcaSummary(
    decimal AvgFillRate,
    decimal AvgSlippageBps,
    decimal TotalEstimatedCost,
    int PoorQualityCount,
    int BlockedIntentCount);

public sealed record TcaAnalysisResult(
    IReadOnlyList<TcaFillMetric> FillMetrics,
    IReadOnlyList<TcaRouteSummary> RouteSummaries,
    TcaSummary Summary);

public sealed record RoutingPolicyRecommendation(
    string Scope,
    string CurrentRoute,
    string ProposedRoute,
    string Priority,
    decimal Confidence,
    string Rationale,
    string GuardrailDecision,
    string GuardrailReason);

public sealed record FeedbackLoopSummary(
    int RecommendationCount,
    int ApprovedCount,
    int BlockedCount,
    int MonitorCount,
    string PolicyState);

public sealed record FeedbackLoopResult(
    IReadOnlyList<RoutingPolicyRecommendation> Recommendations,
    FeedbackLoopSummary Summary);

public sealed record AgentArenaConfig(
    bool Enabled,
    int NegotiationRounds,
    decimal MaxShiftPerRound,
    decimal MinConvergenceScore);

public sealed record AgentArenaBid(
    int Round,
    string AgentId,
    decimal PriorCapitalShare,
    decimal RequestedCapitalShare,
    decimal GrantedCapitalShare,
    decimal UtilityScore,
    decimal Confidence,
    string Decision,
    string Rationale);

public sealed record AgentArenaBookOutcome(
    string AgentId,
    decimal StartCapitalShare,
    decimal FinalCapitalShare,
    decimal NetShift,
    decimal AvgUtilityScore);

public sealed record AgentArenaSummary(
    bool Enabled,
    int RoundsExecuted,
    int ParticipatingAgents,
    decimal ConvergenceScore,
    string PolicyState);

public sealed record AgentArenaResult(
    IReadOnlyList<AgentArenaBid> Bids,
    IReadOnlyList<AgentArenaBookOutcome> Outcomes,
    AgentArenaSummary Summary);

public sealed record PlatformTelemetry(
    decimal FleetHealthScore,
    int CriticalFlags,
    int WarningFlags,
    int ExecutionIntentCount,
    decimal EstimatedLatencyMs,
    string ControlState);

public sealed record PlatformRunResult(
    DateTime Timestamp,
    IReadOnlyList<CompositeSignal> Signals,
    IReadOnlyList<AllocationDraft> Allocations,
    RiskDecision Risk,
    IReadOnlyList<ExecutionIntent> ExecutionIntents,
    PlatformTelemetry Telemetry,
    IReadOnlyList<StrategyBookAllocationSummary> StrategyBooks,
    IReadOnlyList<PolicyOverrideAuditEntry> PolicyAudit,
    IReadOnlyList<StrategyPluginLifecycleEvent> StrategyLifecycle,
    IncidentSimulationResult IncidentSimulation,
    TcaAnalysisResult TcaAnalysis,
    FeedbackLoopResult FeedbackLoop,
    AgentArenaResult AgentArena);

public sealed record PlatformRunSummary(
    int SignalSymbolCount,
    int AllocationCount,
    int StrategyBookCount,
    bool RiskApproved,
    int BreachCount,
    int ExecutionIntentCount,
    decimal GrossExposure,
    decimal NetExposure,
    decimal Turnover,
    decimal TotalExecutionNotional,
    string TopSignalSymbol,
    decimal TopSignalScore,
    decimal FleetHealthScore,
    string ControlState,
    int AppliedPolicyOverrideCount,
    int PendingPolicyOverrideCount,
    int StrategyLifecycleEvents,
    int IncidentTimelineEvents,
    int IncidentReplayFrames,
    int ActiveIncidentFaults,
    decimal TcaAvgFillRate,
    decimal TcaAvgSlippageBps,
    decimal TcaTotalEstimatedCost,
    int FeedbackRecommendationCount,
    int FeedbackApprovedCount,
    int FeedbackBlockedCount,
    string FeedbackPolicyState,
    int AgentArenaRounds,
    int AgentArenaAgents,
    decimal AgentArenaConvergenceScore,
    string AgentArenaPolicyState);
