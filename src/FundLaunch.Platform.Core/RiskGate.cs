using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class RiskGate
{
    public static RiskDecision Evaluate(IReadOnlyList<AllocationDraft> allocations, RiskLimitConfig limits)
    {
        limits.Validate();

        var breaches = new List<string>();
        var gross = allocations.Sum(x => Math.Abs(x.TargetWeight));
        var net = allocations.Sum(x => x.TargetWeight);
        var turnover = allocations.Sum(x => Math.Abs(x.DeltaWeight));

        if (gross > limits.MaxGrossExposure + 0.000001m)
        {
            breaches.Add($"GrossExposure:{gross:F6}>{limits.MaxGrossExposure:F6}");
        }

        if (Math.Abs(net) > limits.MaxAbsNetExposure + 0.000001m)
        {
            breaches.Add($"NetExposure:{Math.Abs(net):F6}>{limits.MaxAbsNetExposure:F6}");
        }

        if (turnover > limits.MaxTurnover + 0.000001m)
        {
            breaches.Add($"Turnover:{turnover:F6}>{limits.MaxTurnover:F6}");
        }

        foreach (var allocation in allocations)
        {
            if (Math.Abs(allocation.TargetWeight) > limits.MaxAbsWeightPerSymbol + 0.000001m)
            {
                breaches.Add($"SymbolCap:{allocation.Symbol}:{Math.Abs(allocation.TargetWeight):F6}>{limits.MaxAbsWeightPerSymbol:F6}");
            }
        }

        var approved = breaches.Count == 0;

        return new RiskDecision(
            Approved: approved,
            Code: approved ? "APPROVED" : "REJECTED",
            Detail: approved ? "All limits satisfied." : string.Join("; ", breaches),
            GrossExposure: Round6(gross),
            NetExposure: Round6(net),
            Turnover: Round6(turnover),
            Breaches: breaches);
    }

    private static decimal Round6(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
