using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class ExecutionPlanner
{
    public static IReadOnlyList<ExecutionIntent> Build(
        IReadOnlyList<AllocationDraft> allocations,
        RiskDecision risk,
        RiskLimitConfig limits)
    {
        if (!risk.Approved)
        {
            return Array.Empty<ExecutionIntent>();
        }

        return allocations
            .Where(x => Math.Abs(x.DeltaWeight) > 0.000001m)
            .Select(x =>
            {
                var notional = Round6(Math.Abs(x.DeltaWeight) * limits.CapitalBase);
                var urgency = Math.Abs(x.DeltaWeight) >= 0.10m ? "HIGH" : Math.Abs(x.DeltaWeight) >= 0.05m ? "MEDIUM" : "LOW";
                var route = notional >= 125000m ? "LIT_SMART" : "INTERNAL_CROSS";

                return new ExecutionIntent(
                    Symbol: x.Symbol,
                    Side: x.Action,
                    DeltaWeight: x.DeltaWeight,
                    Notional: notional,
                    Route: route,
                    Urgency: urgency,
                    StrategyBookId: x.StrategyBookId);
            })
            .Where(x => x.Notional >= limits.MinOrderNotional)
            .OrderByDescending(x => x.Notional)
            .ThenBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static decimal Round6(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
