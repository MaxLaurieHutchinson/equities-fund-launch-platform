using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class CapitalAllocator
{
    public static IReadOnlyList<AllocationDraft> Build(
        IReadOnlyList<CompositeSignal> signals,
        IReadOnlyList<CurrentBookWeight> currentBook,
        RiskLimitConfig limits)
    {
        limits.Validate();

        var currentMap = currentBook.ToDictionary(
            x => x.Symbol.Trim().ToUpperInvariant(),
            x => Round6(x.Weight),
            StringComparer.OrdinalIgnoreCase);

        var rawMap = signals.ToDictionary(x => x.Symbol, x => x.CompositeScore, StringComparer.OrdinalIgnoreCase);

        var signalBias = rawMap.Count == 0 ? 0m : rawMap.Values.Average();
        foreach (var key in rawMap.Keys.ToArray())
        {
            rawMap[key] = rawMap[key] - signalBias;
        }

        var totalAbs = rawMap.Sum(x => Math.Abs(x.Value));

        var targetMap = rawMap.ToDictionary(
            x => x.Key,
            x => totalAbs <= 0m
                ? 0m
                : Round6((x.Value / totalAbs) * limits.MaxGrossExposure),
            StringComparer.OrdinalIgnoreCase);

        foreach (var key in targetMap.Keys.ToArray())
        {
            targetMap[key] = Clamp(targetMap[key], -limits.MaxAbsWeightPerSymbol, limits.MaxAbsWeightPerSymbol);
        }

        var gross = targetMap.Sum(x => Math.Abs(x.Value));
        if (gross > limits.MaxGrossExposure && gross > 0m)
        {
            var scale = limits.MaxGrossExposure / gross;
            foreach (var key in targetMap.Keys.ToArray())
            {
                targetMap[key] = Round6(targetMap[key] * scale);
            }
        }

        var symbols = targetMap.Keys
            .Union(currentMap.Keys, StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var drafts = new List<AllocationDraft>(symbols.Length);
        foreach (var symbol in symbols)
        {
            currentMap.TryGetValue(symbol, out var currentWeight);
            targetMap.TryGetValue(symbol, out var targetWeight);
            var delta = Round6(targetWeight - currentWeight);
            var action = Math.Abs(delta) <= 0.000001m ? "HOLD" : delta > 0m ? "BUY" : "SELL";

            var rationale = action == "HOLD"
                ? "No material change required."
                : $"Rebalance to align with aggregated score for {symbol}.";

            drafts.Add(new AllocationDraft(
                Symbol: symbol,
                CurrentWeight: currentWeight,
                TargetWeight: targetWeight,
                DeltaWeight: delta,
                Action: action,
                Rationale: rationale));
        }

        return drafts;
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
}
