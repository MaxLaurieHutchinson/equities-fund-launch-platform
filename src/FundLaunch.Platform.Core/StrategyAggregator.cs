using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class StrategyAggregator
{
    public static IReadOnlyList<CompositeSignal> Build(IReadOnlyList<StrategySignal> signals)
    {
        return signals
            .Select(Normalize)
            .GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var contributors = group
                    .Select(x => x.StrategyId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var score = group.Sum(x => x.AlphaScore * x.Confidence);

                return new CompositeSignal(
                    Symbol: group.Key,
                    CompositeScore: Round6(score),
                    Contributors: contributors);
            })
            .ToArray();
    }

    private static StrategySignal Normalize(StrategySignal signal)
    {
        return signal with
        {
            StrategyId = signal.StrategyId.Trim().ToUpperInvariant(),
            Symbol = signal.Symbol.Trim().ToUpperInvariant(),
            AlphaScore = Round6(Math.Max(-1m, Math.Min(1m, signal.AlphaScore))),
            Confidence = Round6(Math.Max(0m, Math.Min(1m, signal.Confidence)))
        };
    }

    private static decimal Round6(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
