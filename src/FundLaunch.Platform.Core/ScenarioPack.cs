using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public sealed record FundLaunchScenario(
    IReadOnlyList<StrategySignal> Signals,
    IReadOnlyList<CurrentBookWeight> CurrentBook,
    RiskLimitConfig Limits,
    IReadOnlyList<StrategyBookConfig>? StrategyBooks = null,
    IReadOnlyList<PolicyOverrideRequest>? PolicyOverrides = null,
    StrategyPluginRegistry? PluginRegistry = null);

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

        var strategyBooks = new List<StrategyBookConfig>
        {
            new(
                BookId: "BOOK_MOMENTUM",
                StrategyIds: new[] { "TREND_CORE", "QUALITY_LONG" },
                CapitalShare: 0.55m,
                CurrentBook: new[]
                {
                    new CurrentBookWeight("AAPL", 0.06m),
                    new CurrentBookWeight("MSFT", 0.05m),
                    new CurrentBookWeight("NVDA", 0.04m),
                    new CurrentBookWeight("AMZN", 0.01m)
                }),
            new(
                BookId: "BOOK_RELATIVE_VALUE",
                StrategyIds: new[] { "MEAN_REV", "MACRO_REGIME" },
                CapitalShare: 0.45m,
                CurrentBook: new[]
                {
                    new CurrentBookWeight("AAPL", 0.03m),
                    new CurrentBookWeight("MSFT", 0.03m),
                    new CurrentBookWeight("META", -0.03m),
                    new CurrentBookWeight("XOM", 0.01m),
                    new CurrentBookWeight("AMZN", 0.01m)
                })
        };

        var policyOverrides = new List<PolicyOverrideRequest>
        {
            new(
                PolicyKey: "MaxTurnover",
                Value: 0.78m,
                Reason: "Temporary expansion for launch stabilization.",
                RequestedBy: "OPS_DUTY",
                RequestedAtUtc: new DateTime(2026, 02, 20, 08, 45, 00, DateTimeKind.Utc),
                ApprovedBy: "RISK_CHAIR",
                ApprovedAtUtc: new DateTime(2026, 02, 20, 09, 00, 00, DateTimeKind.Utc),
                ExpiresAtUtc: new DateTime(2099, 12, 31, 00, 00, 00, DateTimeKind.Utc)),
            new(
                PolicyKey: "MaxGrossExposure",
                Value: 1.02m,
                Reason: "Requested ahead of month-end rebalance window.",
                RequestedBy: "PM_DESK",
                RequestedAtUtc: new DateTime(2026, 02, 20, 10, 15, 00, DateTimeKind.Utc)),
            new(
                PolicyKey: "MaxAbsNetExposure",
                Value: 0.28m,
                Reason: "Prior event override no longer active.",
                RequestedBy: "OPS_DUTY",
                RequestedAtUtc: new DateTime(2025, 12, 12, 07, 15, 00, DateTimeKind.Utc),
                ApprovedBy: "RISK_CHAIR",
                ApprovedAtUtc: new DateTime(2025, 12, 12, 07, 20, 00, DateTimeKind.Utc),
                ExpiresAtUtc: new DateTime(2026, 01, 31, 23, 59, 59, DateTimeKind.Utc))
        };

        return new FundLaunchScenario(
            Signals: signals,
            CurrentBook: currentBook,
            Limits: limits,
            StrategyBooks: strategyBooks,
            PolicyOverrides: policyOverrides,
            PluginRegistry: StrategyPluginFactory.CreateDeterministicRegistry());
    }
}
