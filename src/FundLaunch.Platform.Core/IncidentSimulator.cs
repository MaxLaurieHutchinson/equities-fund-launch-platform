using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class IncidentSimulator
{
    public static IncidentSimulationResult Run(
        IReadOnlyList<CompositeSignal> signals,
        IReadOnlyList<ExecutionIntent> baselineIntents,
        IncidentSimulationConfig? config,
        DateTime timestamp,
        IRuntimeEventBus? eventBus = null)
    {
        var normalizedConfig = NormalizeConfig(config);
        eventBus ??= new InMemoryRuntimeEventBus();

        var regime = SelectRegime(signals);
        eventBus.Publish(
            eventType: "REGIME_SELECTED",
            source: "MARKET_REGIME_SIMULATOR",
            detail: $"{regime.Regime} regime selected. Spread={regime.SpreadBps:F1}bps",
            impactScore: regime.VolatilityMultiplier,
            timestamp: timestamp);

        var adjusted = baselineIntents.ToArray();
        var activeFaults = new List<string>();
        var rejectedNotional = 0m;
        var addedLatencyMs = Round6(4m * regime.VolatilityMultiplier);

        if (normalizedConfig.EnableLatencySpike && adjusted.Length > 0)
        {
            activeFaults.Add("LATENCY_SPIKE");
            addedLatencyMs += Round6((10m + adjusted.Length) * Math.Max(1m, normalizedConfig.LatencySpikeMultiplier));

            eventBus.Publish(
                eventType: "FAULT_INJECTED",
                source: "INCIDENT_SIMULATOR",
                detail: $"Latency spike injected (x{normalizedConfig.LatencySpikeMultiplier:F2}).",
                impactScore: normalizedConfig.LatencySpikeMultiplier,
                timestamp: timestamp);

            for (var i = 0; i < adjusted.Length; i++)
            {
                var intent = adjusted[i];
                if (string.Equals(intent.Route, "REJECTED_BY_VENUE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(intent.Urgency, "HIGH", StringComparison.OrdinalIgnoreCase))
                {
                    adjusted[i] = intent with { Route = "LIT_SMART_FAILOVER" };
                }
                else if (string.Equals(intent.Urgency, "MEDIUM", StringComparison.OrdinalIgnoreCase))
                {
                    adjusted[i] = intent with { Route = "INTERNAL_CROSS_FAILOVER" };
                }
            }
        }

        if (normalizedConfig.EnableVenueRejectBurst && adjusted.Length > 0)
        {
            activeFaults.Add("VENUE_REJECT_BURST");
            var rejectCount = ComputeAffectedCount(adjusted.Length, normalizedConfig.VenueRejectRatio);
            var rejectIndices = Enumerable.Range(0, adjusted.Length)
                .OrderByDescending(i => adjusted[i].Notional)
                .ThenBy(i => adjusted[i].Symbol, StringComparer.OrdinalIgnoreCase)
                .Take(rejectCount)
                .ToArray();

            foreach (var index in rejectIndices)
            {
                var prior = adjusted[index];
                if (prior.Notional <= 0m)
                {
                    continue;
                }

                rejectedNotional += prior.Notional;
                adjusted[index] = prior with
                {
                    Notional = 0m,
                    Route = "REJECTED_BY_VENUE",
                    Urgency = "BLOCKED"
                };

                eventBus.Publish(
                    eventType: "ORDER_REJECTED",
                    source: "VENUE_ADAPTER",
                    detail: $"{prior.Symbol} rejected by venue burst protection.",
                    impactScore: prior.Notional,
                    timestamp: timestamp);
            }
        }

        if (normalizedConfig.EnableFeedDropout && adjusted.Length > 0)
        {
            activeFaults.Add("FEED_DROPOUT");
            var affectedCount = ComputeAffectedCount(adjusted.Length, normalizedConfig.FeedDropoutRatio);
            var affectedIndices = Enumerable.Range(0, adjusted.Length)
                .OrderBy(i => adjusted[i].Symbol, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => adjusted[i].StrategyBookId, StringComparer.OrdinalIgnoreCase)
                .Take(affectedCount)
                .ToArray();

            foreach (var index in affectedIndices)
            {
                var prior = adjusted[index];
                if (prior.Notional <= 0m)
                {
                    continue;
                }

                var trimmedNotional = Round6(prior.Notional * (1m - Math.Max(0m, Math.Min(1m, normalizedConfig.FeedDropoutRatio))));
                adjusted[index] = prior with
                {
                    Notional = Math.Max(0m, trimmedNotional),
                    Route = trimmedNotional <= 0m ? "CANCELLED_FEED_GAP" : "SAFE_PASSIVE",
                    Urgency = "LOW"
                };

                eventBus.Publish(
                    eventType: "FEED_DEGRADED",
                    source: "MARKET_DATA_GATEWAY",
                    detail: $"{prior.Symbol} downgraded due to feed dropout.",
                    impactScore: normalizedConfig.FeedDropoutRatio,
                    timestamp: timestamp);
            }
        }

        var replayFrames = BuildReplayFrames(baselineIntents, adjusted);

        eventBus.Publish(
            eventType: "REPLAY_READY",
            source: "INCIDENT_SIMULATOR",
            detail: $"Replay frames ready: {replayFrames.Count}.",
            impactScore: replayFrames.Count,
            timestamp: timestamp);

        return new IncidentSimulationResult(
            Regime: regime,
            Timeline: eventBus.Snapshot(),
            ActiveFaults: activeFaults
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            AdjustedIntents: adjusted,
            ReplayFrames: replayFrames,
            RejectedNotional: Round6(rejectedNotional),
            AddedLatencyMs: Round6(addedLatencyMs));
    }

    private static IncidentSimulationConfig NormalizeConfig(IncidentSimulationConfig? config)
    {
        if (config is null)
        {
            return new IncidentSimulationConfig(
                EnableLatencySpike: false,
                EnableVenueRejectBurst: false,
                EnableFeedDropout: false,
                LatencySpikeMultiplier: 1m,
                VenueRejectRatio: 0m,
                FeedDropoutRatio: 0m);
        }

        return config with
        {
            LatencySpikeMultiplier = Round6(Math.Max(0.25m, config.LatencySpikeMultiplier)),
            VenueRejectRatio = Round6(Math.Max(0m, Math.Min(1m, config.VenueRejectRatio))),
            FeedDropoutRatio = Round6(Math.Max(0m, Math.Min(1m, config.FeedDropoutRatio)))
        };
    }

    private static MarketRegimeSnapshot SelectRegime(IReadOnlyList<CompositeSignal> signals)
    {
        var avgAbsScore = signals.Count == 0
            ? 0m
            : signals.Average(x => Math.Abs(x.CompositeScore));

        if (avgAbsScore >= 0.80m)
        {
            return new MarketRegimeSnapshot(
                Regime: "STRESS",
                VolatilityMultiplier: 1.65m,
                LiquidityMultiplier: 0.62m,
                SpreadBps: 19.5m);
        }

        if (avgAbsScore >= 0.45m)
        {
            return new MarketRegimeSnapshot(
                Regime: "VOLATILE",
                VolatilityMultiplier: 1.30m,
                LiquidityMultiplier: 0.78m,
                SpreadBps: 11.2m);
        }

        return new MarketRegimeSnapshot(
            Regime: "CALM",
            VolatilityMultiplier: 1.05m,
            LiquidityMultiplier: 1.00m,
            SpreadBps: 5.1m);
    }

    private static IReadOnlyList<IncidentReplayFrame> BuildReplayFrames(
        IReadOnlyList<ExecutionIntent> baseline,
        IReadOnlyList<ExecutionIntent> adjusted)
    {
        var frameCount = Math.Min(baseline.Count, adjusted.Count);
        var frames = new List<IncidentReplayFrame>(frameCount);

        for (var i = 0; i < frameCount; i++)
        {
            var before = baseline[i];
            var after = adjusted[i];

            var outcome = "UNCHANGED";
            if (after.Notional <= 0m && before.Notional > 0m)
            {
                outcome = "REJECTED";
            }
            else if (after.Notional < before.Notional)
            {
                outcome = "THROTTLED";
            }
            else if (!string.Equals(before.Route, after.Route, StringComparison.OrdinalIgnoreCase))
            {
                outcome = "REROUTED";
            }

            frames.Add(new IncidentReplayFrame(
                Step: i + 1,
                Symbol: before.Symbol,
                BaselineNotional: before.Notional,
                AdjustedNotional: after.Notional,
                BaselineRoute: before.Route,
                AdjustedRoute: after.Route,
                Outcome: outcome));
        }

        return frames;
    }

    private static int ComputeAffectedCount(int totalCount, decimal ratio)
    {
        if (totalCount <= 0 || ratio <= 0m)
        {
            return 0;
        }

        var count = (int)Math.Floor(totalCount * ratio);
        count = Math.Max(1, count);
        return Math.Min(totalCount, count);
    }

    private static decimal Round6(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
