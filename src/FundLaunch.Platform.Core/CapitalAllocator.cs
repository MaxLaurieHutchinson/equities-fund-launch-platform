using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public sealed record MultiBookAllocationResult(
    IReadOnlyList<AllocationDraft> PortfolioAllocations,
    IReadOnlyList<StrategyBookAllocationSummary> BookSummaries);

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

    public static MultiBookAllocationResult BuildForStrategyBooks(
        IReadOnlyList<StrategySignal> strategySignals,
        IReadOnlyList<StrategyBookConfig> strategyBooks,
        RiskLimitConfig limits)
    {
        limits.Validate();

        var books = NormalizeBooks(strategyBooks);
        if (books.Count == 0)
        {
            return new MultiBookAllocationResult(
                PortfolioAllocations: Array.Empty<AllocationDraft>(),
                BookSummaries: Array.Empty<StrategyBookAllocationSummary>());
        }

        var totalCapitalShare = books.Sum(x => x.CapitalShare);
        if (totalCapitalShare <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(strategyBooks), "Total strategy book capital share must be positive.");
        }

        var allBookDrafts = new List<AllocationDraft>();
        var summaries = new List<StrategyBookAllocationSummary>(books.Count);

        foreach (var book in books)
        {
            var normalizedShare = Round6(book.CapitalShare / totalCapitalShare);
            var strategyIdSet = new HashSet<string>(book.StrategyIds, StringComparer.OrdinalIgnoreCase);

            var bookSignals = strategySignals
                .Where(x => strategyIdSet.Contains(x.StrategyId.Trim().ToUpperInvariant()))
                .ToArray();

            var compositeSignals = StrategyAggregator.Build(bookSignals);
            var bookLimits = BuildBookLimits(limits, normalizedShare);
            var drafts = Build(compositeSignals, book.CurrentBook, bookLimits)
                .Select(x => x with { StrategyBookId = book.BookId })
                .ToArray();

            allBookDrafts.AddRange(drafts);

            summaries.Add(new StrategyBookAllocationSummary(
                BookId: book.BookId,
                CapitalShare: normalizedShare,
                AllocationCount: drafts.Length,
                GrossExposure: Round6(drafts.Sum(x => Math.Abs(x.TargetWeight))),
                NetExposure: Round6(drafts.Sum(x => x.TargetWeight)),
                Turnover: Round6(drafts.Sum(x => Math.Abs(x.DeltaWeight)))));
        }

        var portfolioAllocations = RollUpPortfolioAllocations(allBookDrafts);

        return new MultiBookAllocationResult(
            PortfolioAllocations: portfolioAllocations,
            BookSummaries: summaries.OrderBy(x => x.BookId, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static IReadOnlyList<AllocationDraft> RollUpPortfolioAllocations(
        IReadOnlyList<AllocationDraft> bookDrafts)
    {
        return bookDrafts
            .GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var books = group
                    .Select(x => x.StrategyBookId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var current = Round6(group.Sum(x => x.CurrentWeight));
                var target = Round6(group.Sum(x => x.TargetWeight));
                var delta = Round6(target - current);
                var action = Math.Abs(delta) <= 0.000001m ? "HOLD" : delta > 0m ? "BUY" : "SELL";

                var rationale = action == "HOLD"
                    ? $"No material multi-book change required ({string.Join('|', books)})."
                    : $"Roll-up from strategy books ({string.Join('|', books)}).";

                var strategyBookId = books.Length == 1 ? books[0] : "MULTI_BOOK";

                return new AllocationDraft(
                    Symbol: group.Key,
                    CurrentWeight: current,
                    TargetWeight: target,
                    DeltaWeight: delta,
                    Action: action,
                    Rationale: rationale,
                    StrategyBookId: strategyBookId);
            })
            .ToArray();
    }

    private static RiskLimitConfig BuildBookLimits(RiskLimitConfig limits, decimal capitalShare)
    {
        var boundedShare = Math.Max(0.0001m, Math.Min(1m, capitalShare));

        return new RiskLimitConfig(
            MaxAbsWeightPerSymbol: Round6(limits.MaxAbsWeightPerSymbol * boundedShare),
            MaxGrossExposure: Round6(limits.MaxGrossExposure * boundedShare),
            MaxTurnover: Round6(limits.MaxTurnover * boundedShare),
            MaxAbsNetExposure: Round6(limits.MaxAbsNetExposure * boundedShare),
            MinOrderNotional: limits.MinOrderNotional,
            CapitalBase: Round6(limits.CapitalBase * boundedShare));
    }

    private static IReadOnlyList<StrategyBookConfig> NormalizeBooks(IReadOnlyList<StrategyBookConfig> books)
    {
        return books
            .Select(book =>
            {
                var normalizedStrategies = book.StrategyIds
                    .Select(x => x.Trim().ToUpperInvariant())
                    .Where(x => x.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var normalizedBook = new StrategyBookConfig(
                    BookId: book.BookId.Trim().ToUpperInvariant(),
                    StrategyIds: normalizedStrategies,
                    CapitalShare: Round6(Math.Max(0m, book.CapitalShare)),
                    CurrentBook: book.CurrentBook);

                return normalizedBook;
            })
            .Where(x => x.StrategyIds.Count > 0 && x.CapitalShare > 0m)
            .OrderBy(x => x.BookId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
