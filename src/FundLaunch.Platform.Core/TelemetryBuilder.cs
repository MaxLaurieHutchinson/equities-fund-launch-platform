using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class TelemetryBuilder
{
    public static PlatformTelemetry Build(
        IReadOnlyList<AllocationDraft> allocations,
        RiskDecision risk,
        IReadOnlyList<ExecutionIntent> intents,
        IncidentSimulationResult? incident = null)
    {
        var criticalFlags = risk.Approved ? 0 : Math.Max(1, risk.Breaches.Count);
        var warningFlags = allocations.Count(x => Math.Abs(x.DeltaWeight) >= 0.07m && Math.Abs(x.DeltaWeight) < 0.10m);

        if (incident is not null)
        {
            criticalFlags += incident.ActiveFaults.Count(x =>
                string.Equals(x, "VENUE_REJECT_BURST", StringComparison.OrdinalIgnoreCase));

            warningFlags += incident.ActiveFaults.Count(x =>
                !string.Equals(x, "VENUE_REJECT_BURST", StringComparison.OrdinalIgnoreCase));
        }

        var fleetScore = risk.Approved
            ? 90m - (warningFlags * 2m)
            : 55m - (criticalFlags * 3m);

        if (incident is not null)
        {
            fleetScore -= incident.ActiveFaults.Count * 1.5m;
        }

        fleetScore = Math.Max(0m, Math.Min(100m, fleetScore));

        var estimatedLatency = 18m + (intents.Count * 2.4m) + (criticalFlags * 9m);
        if (incident is not null)
        {
            estimatedLatency += incident.AddedLatencyMs;
        }

        var controlState = risk.Approved ? "RUNNING" : "SAFE_MODE";
        if (risk.Approved && incident is not null && incident.ActiveFaults.Count > 0)
        {
            controlState = "DEGRADED";
        }

        return new PlatformTelemetry(
            FleetHealthScore: Round6(fleetScore),
            CriticalFlags: criticalFlags,
            WarningFlags: warningFlags,
            ExecutionIntentCount: intents.Count,
            EstimatedLatencyMs: Round6(estimatedLatency),
            ControlState: controlState);
    }

    private static decimal Round6(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
