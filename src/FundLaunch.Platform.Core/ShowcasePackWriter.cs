using System.Globalization;
using System.Text;
using System.Text.Json;
using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class ShowcasePackWriter
{
    public static void WritePublicSnapshot(string outputDir, PlatformRunResult run)
    {
        Directory.CreateDirectory(outputDir);

        var summary = FundLaunchEngine.BuildSummary(run);
        var symbolMap = BuildAliasMap(CollectSymbols(run), "EQ");
        var bookMap = BuildAliasMap(CollectStrategyBooks(run), "BOOK");
        var strategyMap = BuildAliasMap(CollectStrategies(run), "STRAT");

        var reportPath = Path.Combine(outputDir, "public-run-report.md");
        var summaryPath = Path.Combine(outputDir, "public-run-summary.json");
        var intentsPath = Path.Combine(outputDir, "public-execution-intents.csv");
        var feedbackPath = Path.Combine(outputDir, "public-feedback-recommendations.csv");
        var timelinePath = Path.Combine(outputDir, "public-event-timeline.csv");
        var lifecyclePath = Path.Combine(outputDir, "public-strategy-lifecycle.csv");
        var arenaPath = Path.Combine(outputDir, "public-agent-arena-bids.csv");

        var publicSummary = new
        {
            run_timestamp_utc = run.Timestamp,
            summary.SignalSymbolCount,
            summary.StrategyBookCount,
            summary.RiskApproved,
            summary.ExecutionIntentCount,
            summary.GrossExposure,
            summary.NetExposure,
            summary.Turnover,
            summary.TotalExecutionNotional,
            summary.ControlState,
            summary.IncidentTimelineEvents,
            summary.ActiveIncidentFaults,
            summary.TcaAvgFillRate,
            summary.TcaAvgSlippageBps,
            summary.TcaTotalEstimatedCost,
            summary.FeedbackRecommendationCount,
            summary.FeedbackApprovedCount,
            summary.FeedbackBlockedCount,
            summary.FeedbackPolicyState,
            summary.AgentArenaRounds,
            summary.AgentArenaAgents,
            summary.AgentArenaConvergenceScore,
            summary.AgentArenaPolicyState,
            sanitized_universe = new
            {
                symbols = symbolMap.Count,
                strategy_books = bookMap.Count,
                strategies = strategyMap.Count
            }
        };

        File.WriteAllText(reportPath, BuildPublicMarkdown(summary));
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(publicSummary, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(intentsPath, BuildPublicIntentsCsv(run.ExecutionIntents, symbolMap, bookMap));
        File.WriteAllText(feedbackPath, BuildPublicFeedbackCsv(run.FeedbackLoop.Recommendations, symbolMap, bookMap));
        File.WriteAllText(timelinePath, BuildPublicTimelineCsv(run.IncidentSimulation.Timeline));
        File.WriteAllText(lifecyclePath, BuildPublicLifecycleCsv(run.StrategyLifecycle, strategyMap));
        File.WriteAllText(arenaPath, BuildPublicArenaCsv(run.AgentArena.Bids, bookMap));
    }

    private static string BuildPublicMarkdown(PlatformRunSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Equities Fund Launch Platform - Public Showcase Snapshot");
        sb.AppendLine();
        sb.AppendLine("## Deterministic Runtime Summary");
        sb.AppendLine($"- Signal universe size: `{summary.SignalSymbolCount}`");
        sb.AppendLine($"- Strategy books: `{summary.StrategyBookCount}`");
        sb.AppendLine($"- Risk approved: `{summary.RiskApproved}`");
        sb.AppendLine($"- Execution intents: `{summary.ExecutionIntentCount}`");
        sb.AppendLine($"- Gross / Net exposure: `{summary.GrossExposure:F4}` / `{summary.NetExposure:F4}`");
        sb.AppendLine($"- Turnover: `{summary.Turnover:F4}`");
        sb.AppendLine($"- Execution notional: `{summary.TotalExecutionNotional:F2}`");
        sb.AppendLine();
        sb.AppendLine("## Incident + Control");
        sb.AppendLine($"- Control state: `{summary.ControlState}`");
        sb.AppendLine($"- Incident events: `{summary.IncidentTimelineEvents}`");
        sb.AppendLine($"- Replay frames: `{summary.IncidentReplayFrames}`");
        sb.AppendLine($"- Active faults: `{summary.ActiveIncidentFaults}`");
        sb.AppendLine();
        sb.AppendLine("## TCA + Feedback");
        sb.AppendLine($"- Avg fill rate: `{summary.TcaAvgFillRate:F4}`");
        sb.AppendLine($"- Avg slippage (bps): `{summary.TcaAvgSlippageBps:F2}`");
        sb.AppendLine($"- Estimated execution cost: `{summary.TcaTotalEstimatedCost:F2}`");
        sb.AppendLine($"- Recommendations (approved/blocked): `{summary.FeedbackApprovedCount}/{summary.FeedbackBlockedCount}`");
        sb.AppendLine($"- Feedback policy state: `{summary.FeedbackPolicyState}`");
        sb.AppendLine();
        sb.AppendLine("## Agent Arena");
        sb.AppendLine($"- Negotiation rounds: `{summary.AgentArenaRounds}`");
        sb.AppendLine($"- Participating agents: `{summary.AgentArenaAgents}`");
        sb.AppendLine($"- Convergence score: `{summary.AgentArenaConvergenceScore:F4}`");
        sb.AppendLine($"- Arena policy state: `{summary.AgentArenaPolicyState}`");

        return sb.ToString();
    }

    private static string BuildPublicIntentsCsv(
        IReadOnlyList<ExecutionIntent> intents,
        IReadOnlyDictionary<string, string> symbolMap,
        IReadOnlyDictionary<string, string> bookMap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("symbol_alias,side,delta_weight,notional,route,urgency,strategy_book_alias");

        foreach (var intent in intents)
        {
            sb.AppendLine(string.Join(',',
                ResolveAlias(symbolMap, intent.Symbol),
                intent.Side,
                intent.DeltaWeight.ToString("F6", CultureInfo.InvariantCulture),
                intent.Notional.ToString("F2", CultureInfo.InvariantCulture),
                intent.Route,
                intent.Urgency,
                ResolveAlias(bookMap, intent.StrategyBookId)));
        }

        return sb.ToString();
    }

    private static string BuildPublicFeedbackCsv(
        IReadOnlyList<RoutingPolicyRecommendation> recommendations,
        IReadOnlyDictionary<string, string> symbolMap,
        IReadOnlyDictionary<string, string> bookMap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("scope_alias,current_route,proposed_route,priority,confidence,guardrail_decision,guardrail_reason");

        foreach (var item in recommendations)
        {
            sb.AppendLine(string.Join(',',
                BuildScopeAlias(item.Scope, symbolMap, bookMap),
                item.CurrentRoute,
                item.ProposedRoute,
                item.Priority,
                item.Confidence.ToString("F4", CultureInfo.InvariantCulture),
                item.GuardrailDecision,
                item.GuardrailReason.Replace(',', ';')));
        }

        return sb.ToString();
    }

    private static string BuildPublicTimelineCsv(IReadOnlyList<RuntimeEvent> timeline)
    {
        var sb = new StringBuilder();
        sb.AppendLine("sequence,timestamp_utc,event_type,source,impact_score");

        foreach (var item in timeline)
        {
            sb.AppendLine(string.Join(',',
                item.Sequence,
                item.Timestamp.ToString("O", CultureInfo.InvariantCulture),
                item.EventType,
                item.Source,
                item.ImpactScore.ToString("F6", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildPublicLifecycleCsv(
        IReadOnlyList<StrategyPluginLifecycleEvent> lifecycle,
        IReadOnlyDictionary<string, string> strategyMap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("strategy_alias,hook,status,timestamp_utc");

        foreach (var item in lifecycle)
        {
            sb.AppendLine(string.Join(',',
                ResolveAlias(strategyMap, item.StrategyId),
                item.Hook,
                item.Status,
                item.Timestamp.ToString("O", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildPublicArenaCsv(
        IReadOnlyList<AgentArenaBid> bids,
        IReadOnlyDictionary<string, string> bookMap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("round,agent_alias,prior_share,requested_share,granted_share,utility_score,confidence,decision");

        foreach (var bid in bids)
        {
            sb.AppendLine(string.Join(',',
                bid.Round,
                ResolveAlias(bookMap, bid.AgentId),
                bid.PriorCapitalShare.ToString("F6", CultureInfo.InvariantCulture),
                bid.RequestedCapitalShare.ToString("F6", CultureInfo.InvariantCulture),
                bid.GrantedCapitalShare.ToString("F6", CultureInfo.InvariantCulture),
                bid.UtilityScore.ToString("F6", CultureInfo.InvariantCulture),
                bid.Confidence.ToString("F4", CultureInfo.InvariantCulture),
                bid.Decision));
        }

        return sb.ToString();
    }

    private static IReadOnlyList<string> CollectSymbols(PlatformRunResult run)
    {
        return run.Signals.Select(x => x.Symbol)
            .Concat(run.Allocations.Select(x => x.Symbol))
            .Concat(run.ExecutionIntents.Select(x => x.Symbol))
            .Concat(run.IncidentSimulation.ReplayFrames.Select(x => x.Symbol))
            .Concat(run.TcaAnalysis.FillMetrics.Select(x => x.Symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> CollectStrategyBooks(PlatformRunResult run)
    {
        return run.StrategyBooks.Select(x => x.BookId)
            .Concat(run.ExecutionIntents.Select(x => x.StrategyBookId))
            .Concat(run.Allocations.Select(x => x.StrategyBookId))
            .Concat(run.TcaAnalysis.FillMetrics.Select(x => x.StrategyBookId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> CollectStrategies(PlatformRunResult run)
    {
        return run.StrategyLifecycle
            .Select(x => x.StrategyId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> BuildAliasMap(
        IReadOnlyList<string> values,
        string prefix)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < values.Count; i++)
        {
            var source = values[i];
            var alias = $"{prefix}{(i + 1):D2}";
            map[source] = alias;
        }

        return map;
    }

    private static string ResolveAlias(IReadOnlyDictionary<string, string> map, string source)
    {
        return map.TryGetValue(source, out var alias) ? alias : "UNMAPPED";
    }

    private static string BuildScopeAlias(
        string scope,
        IReadOnlyDictionary<string, string> symbolMap,
        IReadOnlyDictionary<string, string> bookMap)
    {
        var parts = scope.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return scope.Trim().ToUpperInvariant();
        }

        var symbolAlias = ResolveAlias(symbolMap, parts[0]);
        var bookAlias = ResolveAlias(bookMap, parts[1]);
        return $"{symbolAlias}:{bookAlias}";
    }
}
