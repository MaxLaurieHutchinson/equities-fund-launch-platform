using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class TcaAnalyzer
{
    public static TcaAnalysisResult Analyze(
        IReadOnlyList<ExecutionIntent> baselineIntents,
        IReadOnlyList<ExecutionIntent> executedIntents,
        IncidentSimulationResult incident,
        DateTime timestamp,
        IRuntimeEventBus? eventBus = null)
    {
        eventBus ??= new InMemoryRuntimeEventBus();

        var frameCount = Math.Min(baselineIntents.Count, executedIntents.Count);
        var metrics = new List<TcaFillMetric>(frameCount);

        for (var i = 0; i < frameCount; i++)
        {
            var baseline = baselineIntents[i];
            var executed = executedIntents[i];

            var intended = Math.Max(0m, baseline.Notional);
            var actual = Math.Max(0m, executed.Notional);
            var fillRate = intended <= 0m ? 0m : Round6(actual / intended);
            var slippageBps = ComputeSlippageBps(executed.Route, executed.Urgency, fillRate, incident.Regime);
            var estimatedCost = Round6(actual * (slippageBps / 10000m));
            var qualityBand = DetermineQualityBand(fillRate, slippageBps, actual);

            metrics.Add(new TcaFillMetric(
                Symbol: baseline.Symbol,
                StrategyBookId: baseline.StrategyBookId,
                Route: executed.Route,
                IntendedNotional: intended,
                ExecutedNotional: actual,
                FillRate: fillRate,
                SlippageBps: slippageBps,
                EstimatedCost: estimatedCost,
                QualityBand: qualityBand));
        }

        var routeSummaries = metrics
            .GroupBy(x => x.Route, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TcaRouteSummary(
                Route: group.Key,
                IntentCount: group.Count(),
                AvgFillRate: Round6(group.Average(x => x.FillRate)),
                AvgSlippageBps: Round6(group.Average(x => x.SlippageBps)),
                TotalEstimatedCost: Round6(group.Sum(x => x.EstimatedCost)),
                PoorQualityCount: group.Count(x =>
                    string.Equals(x.QualityBand, "POOR", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.QualityBand, "BLOCKED", StringComparison.OrdinalIgnoreCase))))
            .ToArray();

        var summary = new TcaSummary(
            AvgFillRate: metrics.Count == 0 ? 0m : Round6(metrics.Average(x => x.FillRate)),
            AvgSlippageBps: metrics.Count == 0 ? 0m : Round6(metrics.Average(x => x.SlippageBps)),
            TotalEstimatedCost: Round6(metrics.Sum(x => x.EstimatedCost)),
            PoorQualityCount: metrics.Count(x =>
                string.Equals(x.QualityBand, "POOR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.QualityBand, "BLOCKED", StringComparison.OrdinalIgnoreCase)),
            BlockedIntentCount: metrics.Count(x => string.Equals(x.QualityBand, "BLOCKED", StringComparison.OrdinalIgnoreCase)));

        eventBus.Publish(
            eventType: "TCA_ANALYSIS_READY",
            source: "TCA_ENGINE",
            detail: $"TCA metrics ready: {metrics.Count} intents, avg fill {summary.AvgFillRate:F3}.",
            impactScore: summary.AvgSlippageBps,
            timestamp: timestamp);

        return new TcaAnalysisResult(
            FillMetrics: metrics,
            RouteSummaries: routeSummaries,
            Summary: summary);
    }

    private static decimal ComputeSlippageBps(
        string route,
        string urgency,
        decimal fillRate,
        MarketRegimeSnapshot regime)
    {
        var normalizedRoute = route.Trim().ToUpperInvariant();

        decimal routeBase = normalizedRoute switch
        {
            "INTERNAL_CROSS" => 3.8m,
            "SAFE_PASSIVE" => 4.2m,
            "LIT_SMART" => 6.3m,
            "INTERNAL_CROSS_FAILOVER" => 7.1m,
            "LIT_SMART_FAILOVER" => 9.6m,
            "REJECTED_BY_VENUE" => 42m,
            "CANCELLED_FEED_GAP" => 31m,
            _ => 8.2m
        };

        decimal urgencyAdj = urgency.Trim().ToUpperInvariant() switch
        {
            "HIGH" => 1.6m,
            "MEDIUM" => 0.9m,
            "BLOCKED" => 4m,
            _ => 0.4m
        };

        var fillPenalty = (1m - Math.Max(0m, Math.Min(1m, fillRate))) * 8m;
        var volatilityAdj = regime.VolatilityMultiplier * 2.25m;

        return Round6(routeBase + urgencyAdj + fillPenalty + volatilityAdj);
    }

    private static string DetermineQualityBand(decimal fillRate, decimal slippageBps, decimal executedNotional)
    {
        if (executedNotional <= 0m)
        {
            return "BLOCKED";
        }

        if (fillRate >= 0.95m && slippageBps <= 8m)
        {
            return "STRONG";
        }

        if (fillRate >= 0.85m && slippageBps <= 12m)
        {
            return "GOOD";
        }

        if (fillRate >= 0.70m && slippageBps <= 18m)
        {
            return "DEGRADED";
        }

        return "POOR";
    }

    private static decimal Round6(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
