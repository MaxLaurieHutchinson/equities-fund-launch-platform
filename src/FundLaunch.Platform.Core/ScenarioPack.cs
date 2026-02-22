using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public sealed record CurrentBookWeight(string Symbol, decimal Weight);

public sealed record FundLaunchScenario(
    IReadOnlyList<StrategySignal> Signals,
    IReadOnlyList<CurrentBookWeight> CurrentBook,
    RiskLimitConfig Limits);

public static class FundLaunchScenarioFactory
{
    public static FundLaunchScenario CreateDeterministicScenario()
    {
        var signals = new List<StrategySignal>
        {
            new("TREND_CORE", "AAPL", 0.82m, 0.90m),
            new("TREND_CORE", "MSFT", 0.74m, 0.88m),
            new("TREND_CORE", "NVDA", 0.65m, 0.84m),
            new("MEAN_REV", "AAPL", -0.21m, 0.64m),
            new("MEAN_REV", "AMZN", 0.41m, 0.71m),
            new("MEAN_REV", "META", -0.32m, 0.67m),
            new("MACRO_REGIME", "MSFT", 0.19m, 0.61m),
            new("MACRO_REGIME", "NVDA", 0.22m, 0.58m),
            new("MACRO_REGIME", "XOM", -0.28m, 0.73m),
            new("QUALITY_LONG", "AAPL", 0.36m, 0.76m),
            new("QUALITY_LONG", "MSFT", 0.33m, 0.79m),
            new("QUALITY_LONG", "AMZN", 0.29m, 0.72m)
        };

        var currentBook = new List<CurrentBookWeight>
        {
            new("AAPL", 0.09m),
            new("MSFT", 0.08m),
            new("NVDA", 0.05m),
            new("AMZN", 0.02m),
            new("META", -0.03m),
            new("XOM", 0.01m)
        };

        var limits = new RiskLimitConfig(
            MaxAbsWeightPerSymbol: 0.24m,
            MaxGrossExposure: 0.95m,
            MaxTurnover: 0.70m,
            MaxAbsNetExposure: 0.22m,
            MinOrderNotional: 15000m,
            CapitalBase: 3000000m);

        return new FundLaunchScenario(signals, currentBook, limits);
    }
}
