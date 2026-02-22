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
    IReadOnlyList<StrategyPluginLifecycleEvent> StrategyLifecycle);

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
    int StrategyLifecycleEvents);
