using System.Globalization;
using System.Text;
using System.Text.Json;
using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class ArtifactWriter
{
    public static void Write(string outputDir, PlatformRunResult run)
    {
        Directory.CreateDirectory(outputDir);

        var summary = FundLaunchEngine.BuildSummary(run);

        var markdownPath = Path.Combine(outputDir, "latest-run-report.md");
        var intentsPath = Path.Combine(outputDir, "execution-intents.csv");
        var allocationsPath = Path.Combine(outputDir, "allocations.csv");
        var booksPath = Path.Combine(outputDir, "strategy-books.csv");
        var policyPath = Path.Combine(outputDir, "policy-override-audit.csv");
        var lifecyclePath = Path.Combine(outputDir, "strategy-plugin-lifecycle.csv");
        var incidentTimelinePath = Path.Combine(outputDir, "incident-event-timeline.csv");
        var incidentReplayPath = Path.Combine(outputDir, "incident-replay.csv");
        var incidentSummaryPath = Path.Combine(outputDir, "incident-summary.json");
        var tcaFillPath = Path.Combine(outputDir, "tca-fill-quality.csv");
        var tcaRoutePath = Path.Combine(outputDir, "tca-route-summary.csv");
        var feedbackRecommendationPath = Path.Combine(outputDir, "feedback-recommendations.csv");
        var feedbackSummaryPath = Path.Combine(outputDir, "feedback-loop-summary.json");
        var telemetryPath = Path.Combine(outputDir, "telemetry-dashboard.json");
        var summaryPath = Path.Combine(outputDir, "run-summary.json");

        File.WriteAllText(markdownPath, BuildMarkdown(summary));
        File.WriteAllText(intentsPath, BuildIntentCsv(run.ExecutionIntents));
        File.WriteAllText(allocationsPath, BuildAllocationCsv(run.Allocations));
        File.WriteAllText(booksPath, BuildStrategyBookCsv(run.StrategyBooks));
        File.WriteAllText(policyPath, BuildPolicyAuditCsv(run.PolicyAudit));
        File.WriteAllText(lifecyclePath, BuildStrategyLifecycleCsv(run.StrategyLifecycle));
        File.WriteAllText(incidentTimelinePath, BuildIncidentTimelineCsv(run.IncidentSimulation.Timeline));
        File.WriteAllText(incidentReplayPath, BuildIncidentReplayCsv(run.IncidentSimulation.ReplayFrames));
        File.WriteAllText(incidentSummaryPath, JsonSerializer.Serialize(run.IncidentSimulation, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(tcaFillPath, BuildTcaFillCsv(run.TcaAnalysis.FillMetrics));
        File.WriteAllText(tcaRoutePath, BuildTcaRouteSummaryCsv(run.TcaAnalysis.RouteSummaries));
        File.WriteAllText(feedbackRecommendationPath, BuildFeedbackRecommendationCsv(run.FeedbackLoop.Recommendations));
        File.WriteAllText(feedbackSummaryPath, JsonSerializer.Serialize(run.FeedbackLoop.Summary, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(telemetryPath, JsonSerializer.Serialize(run.Telemetry, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string BuildMarkdown(PlatformRunSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Equities Fund Launch Platform Run Report");
        sb.AppendLine();
        sb.AppendLine($"- Signal symbols: `{summary.SignalSymbolCount}`");
        sb.AppendLine($"- Allocations: `{summary.AllocationCount}`");
        sb.AppendLine($"- Strategy books: `{summary.StrategyBookCount}`");
        sb.AppendLine($"- Risk approved: `{summary.RiskApproved}`");
        sb.AppendLine($"- Breach count: `{summary.BreachCount}`");
        sb.AppendLine($"- Execution intents: `{summary.ExecutionIntentCount}`");
        sb.AppendLine($"- Gross exposure: `{summary.GrossExposure:F4}`");
        sb.AppendLine($"- Net exposure: `{summary.NetExposure:F4}`");
        sb.AppendLine($"- Turnover: `{summary.Turnover:F4}`");
        sb.AppendLine($"- Total execution notional: `{summary.TotalExecutionNotional:F2}`");
        sb.AppendLine($"- Top signal: `{summary.TopSignalSymbol}` (`{summary.TopSignalScore:F4}`)");
        sb.AppendLine($"- Fleet health score: `{summary.FleetHealthScore:F2}`");
        sb.AppendLine($"- Control state: `{summary.ControlState}`");
        sb.AppendLine($"- Applied policy overrides: `{summary.AppliedPolicyOverrideCount}`");
        sb.AppendLine($"- Pending policy overrides: `{summary.PendingPolicyOverrideCount}`");
        sb.AppendLine($"- Strategy lifecycle events: `{summary.StrategyLifecycleEvents}`");
        sb.AppendLine($"- Incident timeline events: `{summary.IncidentTimelineEvents}`");
        sb.AppendLine($"- Incident replay frames: `{summary.IncidentReplayFrames}`");
        sb.AppendLine($"- Active incident faults: `{summary.ActiveIncidentFaults}`");
        sb.AppendLine($"- TCA avg fill rate: `{summary.TcaAvgFillRate:F4}`");
        sb.AppendLine($"- TCA avg slippage (bps): `{summary.TcaAvgSlippageBps:F2}`");
        sb.AppendLine($"- TCA estimated cost: `{summary.TcaTotalEstimatedCost:F2}`");
        sb.AppendLine($"- Feedback recommendations: `{summary.FeedbackRecommendationCount}`");
        sb.AppendLine($"- Feedback approved/blocked: `{summary.FeedbackApprovedCount}/{summary.FeedbackBlockedCount}`");
        sb.AppendLine($"- Feedback policy state: `{summary.FeedbackPolicyState}`");

        return sb.ToString();
    }

    private static string BuildIntentCsv(IReadOnlyList<ExecutionIntent> intents)
    {
        var sb = new StringBuilder();
        sb.AppendLine("symbol,side,delta_weight,notional,route,urgency,strategy_book_id");

        foreach (var intent in intents)
        {
            sb.AppendLine(string.Join(',',
                intent.Symbol,
                intent.Side,
                intent.DeltaWeight.ToString("F6", CultureInfo.InvariantCulture),
                intent.Notional.ToString("F2", CultureInfo.InvariantCulture),
                intent.Route,
                intent.Urgency,
                intent.StrategyBookId));
        }

        return sb.ToString();
    }

    private static string BuildAllocationCsv(IReadOnlyList<AllocationDraft> allocations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("symbol,current_weight,target_weight,delta_weight,action,rationale,strategy_book_id");

        foreach (var allocation in allocations)
        {
            sb.AppendLine(string.Join(',',
                allocation.Symbol,
                allocation.CurrentWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.TargetWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.DeltaWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.Action,
                allocation.Rationale.Replace(',', ';'),
                allocation.StrategyBookId));
        }

        return sb.ToString();
    }

    private static string BuildStrategyBookCsv(IReadOnlyList<StrategyBookAllocationSummary> strategyBooks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("book_id,capital_share,allocation_count,gross_exposure,net_exposure,turnover");

        foreach (var book in strategyBooks)
        {
            sb.AppendLine(string.Join(',',
                book.BookId,
                book.CapitalShare.ToString("F6", CultureInfo.InvariantCulture),
                book.AllocationCount,
                book.GrossExposure.ToString("F6", CultureInfo.InvariantCulture),
                book.NetExposure.ToString("F6", CultureInfo.InvariantCulture),
                book.Turnover.ToString("F6", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildPolicyAuditCsv(IReadOnlyList<PolicyOverrideAuditEntry> auditTrail)
    {
        var sb = new StringBuilder();
        sb.AppendLine("policy_key,requested_value,prior_value,applied_value,status,reason,requested_by,approved_by,requested_at_utc,approved_at_utc,evaluated_at_utc");

        foreach (var entry in auditTrail)
        {
            sb.AppendLine(string.Join(',',
                entry.PolicyKey,
                entry.RequestedValue.ToString("F6", CultureInfo.InvariantCulture),
                entry.PriorValue?.ToString("F6", CultureInfo.InvariantCulture) ?? string.Empty,
                entry.AppliedValue?.ToString("F6", CultureInfo.InvariantCulture) ?? string.Empty,
                entry.Status,
                entry.Reason.Replace(',', ';'),
                entry.RequestedBy,
                entry.ApprovedBy ?? string.Empty,
                entry.RequestedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                entry.ApprovedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                entry.EvaluatedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildStrategyLifecycleCsv(IReadOnlyList<StrategyPluginLifecycleEvent> lifecycle)
    {
        var sb = new StringBuilder();
        sb.AppendLine("strategy_id,hook,status,detail,timestamp_utc");

        foreach (var entry in lifecycle)
        {
            sb.AppendLine(string.Join(',',
                entry.StrategyId,
                entry.Hook,
                entry.Status,
                entry.Detail.Replace(',', ';'),
                entry.Timestamp.ToString("O", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildIncidentTimelineCsv(IReadOnlyList<RuntimeEvent> timeline)
    {
        var sb = new StringBuilder();
        sb.AppendLine("sequence,timestamp_utc,event_type,source,detail,impact_score");

        foreach (var entry in timeline)
        {
            sb.AppendLine(string.Join(',',
                entry.Sequence,
                entry.Timestamp.ToString("O", CultureInfo.InvariantCulture),
                entry.EventType,
                entry.Source,
                entry.Detail.Replace(',', ';'),
                entry.ImpactScore.ToString("F6", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildIncidentReplayCsv(IReadOnlyList<IncidentReplayFrame> replayFrames)
    {
        var sb = new StringBuilder();
        sb.AppendLine("step,symbol,baseline_notional,adjusted_notional,baseline_route,adjusted_route,outcome");

        foreach (var frame in replayFrames)
        {
            sb.AppendLine(string.Join(',',
                frame.Step,
                frame.Symbol,
                frame.BaselineNotional.ToString("F2", CultureInfo.InvariantCulture),
                frame.AdjustedNotional.ToString("F2", CultureInfo.InvariantCulture),
                frame.BaselineRoute,
                frame.AdjustedRoute,
                frame.Outcome));
        }

        return sb.ToString();
    }

    private static string BuildTcaFillCsv(IReadOnlyList<TcaFillMetric> fillMetrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("symbol,strategy_book_id,route,intended_notional,executed_notional,fill_rate,slippage_bps,estimated_cost,quality_band");

        foreach (var metric in fillMetrics)
        {
            sb.AppendLine(string.Join(',',
                metric.Symbol,
                metric.StrategyBookId,
                metric.Route,
                metric.IntendedNotional.ToString("F2", CultureInfo.InvariantCulture),
                metric.ExecutedNotional.ToString("F2", CultureInfo.InvariantCulture),
                metric.FillRate.ToString("F6", CultureInfo.InvariantCulture),
                metric.SlippageBps.ToString("F6", CultureInfo.InvariantCulture),
                metric.EstimatedCost.ToString("F2", CultureInfo.InvariantCulture),
                metric.QualityBand));
        }

        return sb.ToString();
    }

    private static string BuildTcaRouteSummaryCsv(IReadOnlyList<TcaRouteSummary> routeSummaries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("route,intent_count,avg_fill_rate,avg_slippage_bps,total_estimated_cost,poor_quality_count");

        foreach (var route in routeSummaries)
        {
            sb.AppendLine(string.Join(',',
                route.Route,
                route.IntentCount,
                route.AvgFillRate.ToString("F6", CultureInfo.InvariantCulture),
                route.AvgSlippageBps.ToString("F6", CultureInfo.InvariantCulture),
                route.TotalEstimatedCost.ToString("F2", CultureInfo.InvariantCulture),
                route.PoorQualityCount));
        }

        return sb.ToString();
    }

    private static string BuildFeedbackRecommendationCsv(IReadOnlyList<RoutingPolicyRecommendation> recommendations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("scope,current_route,proposed_route,priority,confidence,rationale,guardrail_decision,guardrail_reason");

        foreach (var recommendation in recommendations)
        {
            sb.AppendLine(string.Join(',',
                recommendation.Scope,
                recommendation.CurrentRoute,
                recommendation.ProposedRoute,
                recommendation.Priority,
                recommendation.Confidence.ToString("F4", CultureInfo.InvariantCulture),
                recommendation.Rationale.Replace(',', ';'),
                recommendation.GuardrailDecision,
                recommendation.GuardrailReason.Replace(',', ';')));
        }

        return sb.ToString();
    }
}
