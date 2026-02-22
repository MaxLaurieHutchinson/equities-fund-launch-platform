using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class FeedbackLoopEngine
{
    public static FeedbackLoopResult BuildRecommendations(
        TcaAnalysisResult tca,
        RiskDecision risk,
        IncidentSimulationResult incident,
        DateTime timestamp,
        IRuntimeEventBus? eventBus = null)
    {
        eventBus ??= new InMemoryRuntimeEventBus();

        var candidates = tca.FillMetrics
            .Where(x =>
                string.Equals(x.QualityBand, "DEGRADED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.QualityBand, "POOR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.QualityBand, "BLOCKED", StringComparison.OrdinalIgnoreCase))
            .Select(metric => BuildRecommendation(metric, risk, incident))
            .GroupBy(x => x.Scope, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(x => PriorityRank(x.Priority))
                .ThenByDescending(x => x.Confidence)
                .First())
            .OrderByDescending(x => PriorityRank(x.Priority))
            .ThenBy(x => x.Scope, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates.Add(new RoutingPolicyRecommendation(
                Scope: "GLOBAL",
                CurrentRoute: "UNCHANGED",
                ProposedRoute: "UNCHANGED",
                Priority: "LOW",
                Confidence: 0.58m,
                Rationale: "No low-quality fills detected in current replay window.",
                GuardrailDecision: "MONITOR",
                GuardrailReason: "Observe additional cycles before tuning."));
        }

        var approvedCount = candidates.Count(x => string.Equals(x.GuardrailDecision, "APPROVED", StringComparison.OrdinalIgnoreCase));
        var blockedCount = candidates.Count(x => string.Equals(x.GuardrailDecision, "BLOCKED", StringComparison.OrdinalIgnoreCase));
        var monitorCount = candidates.Count(x => string.Equals(x.GuardrailDecision, "MONITOR", StringComparison.OrdinalIgnoreCase));

        var policyState = approvedCount > 0
            ? "ACTIVE_TUNING"
            : blockedCount > 0
                ? "GUARDRAILED_ONLY"
                : "OBSERVE_ONLY";

        var summary = new FeedbackLoopSummary(
            RecommendationCount: candidates.Count,
            ApprovedCount: approvedCount,
            BlockedCount: blockedCount,
            MonitorCount: monitorCount,
            PolicyState: policyState);

        eventBus.Publish(
            eventType: "FEEDBACK_READY",
            source: "FEEDBACK_LOOP_ENGINE",
            detail: $"Recommendations={summary.RecommendationCount}, approved={summary.ApprovedCount}, blocked={summary.BlockedCount}.",
            impactScore: summary.RecommendationCount,
            timestamp: timestamp);

        return new FeedbackLoopResult(
            Recommendations: candidates,
            Summary: summary);
    }

    private static RoutingPolicyRecommendation BuildRecommendation(
        TcaFillMetric metric,
        RiskDecision risk,
        IncidentSimulationResult incident)
    {
        var proposedRoute = ProposeRoute(metric);
        var priority = SelectPriority(metric.QualityBand, metric.FillRate);
        var confidence = SelectConfidence(metric.QualityBand, metric.FillRate, metric.SlippageBps);

        var guardrailDecision = "APPROVED";
        var guardrailReason = "Within guardrails.";

        if (!risk.Approved)
        {
            guardrailDecision = "BLOCKED";
            guardrailReason = "Risk gate is not approved.";
        }
        else if (string.Equals(incident.Regime.Regime, "STRESS", StringComparison.OrdinalIgnoreCase)
            && string.Equals(proposedRoute, "LIT_SMART", StringComparison.OrdinalIgnoreCase))
        {
            guardrailDecision = "BLOCKED";
            guardrailReason = "Stress regime blocks lit expansion.";
        }
        else if (incident.ActiveFaults.Contains("VENUE_REJECT_BURST", StringComparer.OrdinalIgnoreCase)
            && proposedRoute.Contains("LIT_SMART", StringComparison.OrdinalIgnoreCase))
        {
            guardrailDecision = "BLOCKED";
            guardrailReason = "Venue reject burst active for lit routing.";
        }
        else if (string.Equals(metric.QualityBand, "DEGRADED", StringComparison.OrdinalIgnoreCase))
        {
            guardrailDecision = "MONITOR";
            guardrailReason = "Require one more cycle before route switch.";
        }

        var rationale = $"FillRate={metric.FillRate:F3}, Slippage={metric.SlippageBps:F2}bps, Quality={metric.QualityBand}.";
        var scope = $"{metric.Symbol}:{metric.StrategyBookId}";

        return new RoutingPolicyRecommendation(
            Scope: scope,
            CurrentRoute: metric.Route,
            ProposedRoute: proposedRoute,
            Priority: priority,
            Confidence: confidence,
            Rationale: rationale,
            GuardrailDecision: guardrailDecision,
            GuardrailReason: guardrailReason);
    }

    private static string ProposeRoute(TcaFillMetric metric)
    {
        var route = metric.Route.Trim().ToUpperInvariant();

        if (string.Equals(metric.QualityBand, "BLOCKED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(metric.QualityBand, "POOR", StringComparison.OrdinalIgnoreCase))
        {
            return route switch
            {
                "REJECTED_BY_VENUE" => "INTERNAL_CROSS_FAILOVER",
                "CANCELLED_FEED_GAP" => "SAFE_PASSIVE",
                "LIT_SMART" => "INTERNAL_CROSS",
                _ => "SAFE_PASSIVE"
            };
        }

        return route switch
        {
            "LIT_SMART_FAILOVER" => "INTERNAL_CROSS_FAILOVER",
            "LIT_SMART" => "INTERNAL_CROSS",
            "INTERNAL_CROSS_FAILOVER" => "SAFE_PASSIVE",
            "INTERNAL_CROSS" => "SAFE_PASSIVE",
            "SAFE_PASSIVE" => metric.FillRate < 0.70m ? "INTERNAL_CROSS" : "SAFE_PASSIVE",
            _ => "SAFE_PASSIVE"
        };
    }

    private static string SelectPriority(string qualityBand, decimal fillRate)
    {
        if (string.Equals(qualityBand, "BLOCKED", StringComparison.OrdinalIgnoreCase))
        {
            return "HIGH";
        }

        if (string.Equals(qualityBand, "POOR", StringComparison.OrdinalIgnoreCase) || fillRate < 0.55m)
        {
            return "HIGH";
        }

        if (string.Equals(qualityBand, "DEGRADED", StringComparison.OrdinalIgnoreCase))
        {
            return "MEDIUM";
        }

        return "LOW";
    }

    private static decimal SelectConfidence(string qualityBand, decimal fillRate, decimal slippageBps)
    {
        var qualityScore = string.Equals(qualityBand, "BLOCKED", StringComparison.OrdinalIgnoreCase) ? 0.86m
            : string.Equals(qualityBand, "POOR", StringComparison.OrdinalIgnoreCase) ? 0.79m
            : string.Equals(qualityBand, "DEGRADED", StringComparison.OrdinalIgnoreCase) ? 0.65m
            : 0.55m;

        var fillPenalty = (1m - Math.Max(0m, Math.Min(1m, fillRate))) * 0.22m;
        var slipBoost = Math.Min(0.10m, slippageBps / 200m);

        return Round4(Math.Max(0.45m, Math.Min(0.98m, qualityScore + fillPenalty + slipBoost)));
    }

    private static int PriorityRank(string priority)
    {
        return priority.Trim().ToUpperInvariant() switch
        {
            "HIGH" => 3,
            "MEDIUM" => 2,
            _ => 1
        };
    }

    private static decimal Round4(decimal value)
    {
        return decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }
}
