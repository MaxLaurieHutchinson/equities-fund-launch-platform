using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class AgentArenaEngine
{
    public static AgentArenaResult Run(
        IReadOnlyList<StrategyBookAllocationSummary> strategyBooks,
        TcaAnalysisResult tca,
        FeedbackLoopResult feedback,
        IncidentSimulationResult incident,
        RiskDecision risk,
        AgentArenaConfig? config,
        DateTime timestamp,
        IRuntimeEventBus? eventBus = null)
    {
        eventBus ??= new InMemoryRuntimeEventBus();
        var normalizedConfig = NormalizeConfig(config);

        var participants = strategyBooks
            .OrderBy(x => x.BookId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!normalizedConfig.Enabled || participants.Length == 0)
        {
            return DisabledResult(participants.Length);
        }

        var startShares = NormalizeShares(participants.ToDictionary(
            x => x.BookId,
            x => x.CapitalShare,
            StringComparer.OrdinalIgnoreCase));

        var shares = new Dictionary<string, decimal>(startShares, StringComparer.OrdinalIgnoreCase);
        var bids = new List<AgentArenaBid>();
        var lastRoundShift = 0m;

        eventBus.Publish(
            eventType: "AGENT_ARENA_STARTED",
            source: "AGENT_ARENA_ENGINE",
            detail: $"Agent arena started with {participants.Length} participants.",
            impactScore: participants.Length,
            timestamp: timestamp);

        for (var round = 1; round <= normalizedConfig.NegotiationRounds; round++)
        {
            var requested = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var utilityByBook = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var confidenceByBook = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var rationaleByBook = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var book in participants)
            {
                var prior = shares[book.BookId];
                var perf = BuildPerformanceSnapshot(book.BookId, tca, feedback, incident);

                var desiredShift = perf.UtilityBias - perf.RiskPenalty;
                desiredShift = Clamp(desiredShift, -normalizedConfig.MaxShiftPerRound, normalizedConfig.MaxShiftPerRound);
                var candidate = Math.Max(0.01m, prior + desiredShift);

                requested[book.BookId] = Round6(candidate);
                utilityByBook[book.BookId] = perf.UtilityScore;
                confidenceByBook[book.BookId] = perf.Confidence;
                rationaleByBook[book.BookId] = perf.Rationale;
            }

            var normalizedRequested = NormalizeShares(requested);
            var granted = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var book in participants)
            {
                var prior = shares[book.BookId];
                var requestedShare = normalizedRequested[book.BookId];
                var blended = Round6((prior * 0.35m) + (requestedShare * 0.65m));
                granted[book.BookId] = blended;
            }

            granted = new Dictionary<string, decimal>(NormalizeShares(granted), StringComparer.OrdinalIgnoreCase);

            lastRoundShift = 0m;
            foreach (var book in participants)
            {
                var prior = shares[book.BookId];
                var requestedShare = normalizedRequested[book.BookId];
                var grantedShare = granted[book.BookId];
                lastRoundShift += Math.Abs(grantedShare - prior);

                var decision = grantedShare > prior + 0.000001m
                    ? "INCREASE"
                    : grantedShare < prior - 0.000001m
                        ? "DECREASE"
                        : "HOLD";

                bids.Add(new AgentArenaBid(
                    Round: round,
                    AgentId: book.BookId,
                    PriorCapitalShare: prior,
                    RequestedCapitalShare: requestedShare,
                    GrantedCapitalShare: grantedShare,
                    UtilityScore: utilityByBook[book.BookId],
                    Confidence: confidenceByBook[book.BookId],
                    Decision: decision,
                    Rationale: rationaleByBook[book.BookId]));
            }

            shares = granted;

            eventBus.Publish(
                eventType: "AGENT_ARENA_ROUND",
                source: "AGENT_ARENA_ENGINE",
                detail: $"Round {round} completed; aggregate shift={Round6(lastRoundShift):F4}.",
                impactScore: Round6(lastRoundShift),
                timestamp: timestamp);
        }

        var convergence = ComputeConvergence(lastRoundShift, normalizedConfig.MaxShiftPerRound, participants.Length);
        var policyState = ResolvePolicyState(risk, feedback, convergence, normalizedConfig.MinConvergenceScore);

        var outcomes = participants
            .Select(book =>
            {
                var start = startShares[book.BookId];
                var final = shares[book.BookId];
                var avgUtility = bids
                    .Where(x => x.AgentId.Equals(book.BookId, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.UtilityScore)
                    .DefaultIfEmpty(0m)
                    .Average();

                return new AgentArenaBookOutcome(
                    AgentId: book.BookId,
                    StartCapitalShare: start,
                    FinalCapitalShare: final,
                    NetShift: Round6(final - start),
                    AvgUtilityScore: Round6(avgUtility));
            })
            .OrderBy(x => x.AgentId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var summary = new AgentArenaSummary(
            Enabled: true,
            RoundsExecuted: normalizedConfig.NegotiationRounds,
            ParticipatingAgents: participants.Length,
            ConvergenceScore: convergence,
            PolicyState: policyState);

        eventBus.Publish(
            eventType: "AGENT_ARENA_COMPLETED",
            source: "AGENT_ARENA_ENGINE",
            detail: $"Agent arena completed with state={policyState}.",
            impactScore: convergence,
            timestamp: timestamp);

        return new AgentArenaResult(
            Bids: bids
                .OrderBy(x => x.Round)
                .ThenBy(x => x.AgentId, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Outcomes: outcomes,
            Summary: summary);
    }

    private static AgentArenaResult DisabledResult(int participants)
    {
        return new AgentArenaResult(
            Bids: Array.Empty<AgentArenaBid>(),
            Outcomes: Array.Empty<AgentArenaBookOutcome>(),
            Summary: new AgentArenaSummary(
                Enabled: false,
                RoundsExecuted: 0,
                ParticipatingAgents: participants,
                ConvergenceScore: 0m,
                PolicyState: "DISABLED"));
    }

    private static AgentArenaConfig NormalizeConfig(AgentArenaConfig? config)
    {
        if (config is null)
        {
            return new AgentArenaConfig(
                Enabled: false,
                NegotiationRounds: 0,
                MaxShiftPerRound: 0m,
                MinConvergenceScore: 0m);
        }

        return config with
        {
            NegotiationRounds = Math.Max(1, config.NegotiationRounds),
            MaxShiftPerRound = Round6(Math.Max(0.01m, Math.Min(0.25m, config.MaxShiftPerRound))),
            MinConvergenceScore = Round6(Math.Max(0.50m, Math.Min(0.99m, config.MinConvergenceScore)))
        };
    }

    private static (decimal UtilityScore, decimal UtilityBias, decimal RiskPenalty, decimal Confidence, string Rationale)
        BuildPerformanceSnapshot(
            string bookId,
            TcaAnalysisResult tca,
            FeedbackLoopResult feedback,
            IncidentSimulationResult incident)
    {
        var metrics = tca.FillMetrics
            .Where(x => x.StrategyBookId.Equals(bookId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var avgFill = metrics.Length == 0 ? 0.70m : metrics.Average(x => x.FillRate);
        var avgSlippage = metrics.Length == 0 ? 10m : metrics.Average(x => x.SlippageBps);
        var poorCount = metrics.Count(x =>
            string.Equals(x.QualityBand, "POOR", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.QualityBand, "BLOCKED", StringComparison.OrdinalIgnoreCase));

        var approved = feedback.Recommendations.Count(x =>
            x.Scope.EndsWith($":{bookId}", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.GuardrailDecision, "APPROVED", StringComparison.OrdinalIgnoreCase));

        var blocked = feedback.Recommendations.Count(x =>
            x.Scope.EndsWith($":{bookId}", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.GuardrailDecision, "BLOCKED", StringComparison.OrdinalIgnoreCase));

        var incidentPenalty = incident.ActiveFaults.Count * 0.012m;
        var utilityScore = Round6((avgFill * 100m) - (avgSlippage * 2.4m) - (poorCount * 4m) + (approved * 3m) - (blocked * 5m));
        var utilityBias = Round6(((avgFill - 0.72m) * 0.18m) - ((avgSlippage - 10m) / 500m) + (approved * 0.02m) - (blocked * 0.02m));
        var riskPenalty = Round6(incidentPenalty + (poorCount * 0.01m));
        var confidence = Round4(Clamp(0.55m + (avgFill * 0.25m) - (blocked * 0.04m), 0.35m, 0.98m));
        var rationale = $"Fill={avgFill:F3}, Slip={avgSlippage:F2}bps, Poor={poorCount}, Approved={approved}, Blocked={blocked}.";

        return (utilityScore, utilityBias, riskPenalty, confidence, rationale);
    }

    private static Dictionary<string, decimal> NormalizeShares(IDictionary<string, decimal> shares)
    {
        var total = shares.Sum(x => Math.Max(0m, x.Value));
        var normalized = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        if (total <= 0m)
        {
            var equal = shares.Count == 0 ? 0m : Round6(1m / shares.Count);
            foreach (var kvp in shares)
            {
                normalized[kvp.Key] = equal;
            }

            return normalized;
        }

        foreach (var kvp in shares)
        {
            normalized[kvp.Key] = Round6(Math.Max(0m, kvp.Value) / total);
        }

        var drift = Round6(1m - normalized.Sum(x => x.Value));
        if (drift != 0m && normalized.Count > 0)
        {
            var firstKey = normalized.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First();
            normalized[firstKey] = Round6(normalized[firstKey] + drift);
        }

        return normalized;
    }

    private static decimal ComputeConvergence(decimal lastRoundShift, decimal maxShift, int participants)
    {
        if (participants <= 0 || maxShift <= 0m)
        {
            return 1m;
        }

        var normalizedShift = lastRoundShift / (participants * maxShift);
        return Round6(Clamp(1m - normalizedShift, 0m, 1m));
    }

    private static string ResolvePolicyState(
        RiskDecision risk,
        FeedbackLoopResult feedback,
        decimal convergence,
        decimal minConvergence)
    {
        if (!risk.Approved)
        {
            return "HALTED";
        }

        if (feedback.Summary.BlockedCount > 0)
        {
            return "GUARDRAILED";
        }

        if (convergence >= minConvergence)
        {
            return "CONVERGED";
        }

        if (convergence >= (minConvergence * 0.75m))
        {
            return "STABILIZING";
        }

        return "DIVERGENT";
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static decimal Round6(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }

    private static decimal Round4(decimal value)
    {
        return decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }
}
