using FundLaunch.Platform.Contracts;
using FundLaunch.Platform.Core;

namespace FundLaunch.Platform.Core.Tests;

public sealed class PhaseTwoRuntimeTests
{
    [Fact]
    public void PolicyOverrideEngine_Applies_Approved_And_Audits_Pending()
    {
        var baseline = new RiskLimitConfig(
            MaxAbsWeightPerSymbol: 0.20m,
            MaxGrossExposure: 0.80m,
            MaxTurnover: 0.45m,
            MaxAbsNetExposure: 0.18m,
            MinOrderNotional: 10000m,
            CapitalBase: 1000000m);

        var overrides = new[]
        {
            new PolicyOverrideRequest(
                PolicyKey: "MaxTurnover",
                Value: 0.60m,
                Reason: "Launch window rebalance.",
                RequestedBy: "OPS",
                RequestedAtUtc: new DateTime(2026, 02, 01, 10, 00, 00, DateTimeKind.Utc),
                ApprovedBy: "RISK",
                ApprovedAtUtc: new DateTime(2026, 02, 01, 10, 05, 00, DateTimeKind.Utc)),
            new PolicyOverrideRequest(
                PolicyKey: "MaxGrossExposure",
                Value: 0.92m,
                Reason: "Pending approval.",
                RequestedBy: "PM",
                RequestedAtUtc: new DateTime(2026, 02, 01, 10, 01, 00, DateTimeKind.Utc)),
            new PolicyOverrideRequest(
                PolicyKey: "UnknownPolicy",
                Value: 1.00m,
                Reason: "Unsupported key.",
                RequestedBy: "OPS",
                RequestedAtUtc: new DateTime(2026, 02, 01, 10, 02, 00, DateTimeKind.Utc),
                ApprovedBy: "RISK",
                ApprovedAtUtc: new DateTime(2026, 02, 01, 10, 03, 00, DateTimeKind.Utc))
        };

        var result = PolicyOverrideEngine.Apply(
            baseline,
            overrides,
            asOfUtc: new DateTime(2026, 02, 22, 12, 00, 00, DateTimeKind.Utc));

        Assert.Equal(0.60m, result.EffectiveLimits.MaxTurnover);
        Assert.Equal(0.80m, result.EffectiveLimits.MaxGrossExposure);
        Assert.Contains(result.AuditTrail, x => x.PolicyKey == "MaxTurnover" && x.Status == "APPLIED");
        Assert.Contains(result.AuditTrail, x => x.PolicyKey == "MaxGrossExposure" && x.Status == "PENDING_APPROVAL");
        Assert.Contains(result.AuditTrail, x => x.PolicyKey == "UnknownPolicy" && x.Status == "UNSUPPORTED_POLICY");
    }

    [Fact]
    public void CapitalAllocator_BuildForStrategyBooks_RollsUp_Portfolio()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();

        var multiBook = CapitalAllocator.BuildForStrategyBooks(
            strategySignals: scenario.Signals,
            strategyBooks: scenario.StrategyBooks!,
            limits: scenario.Limits);

        Assert.Equal(2, multiBook.BookSummaries.Count);
        Assert.NotEmpty(multiBook.PortfolioAllocations);
        Assert.All(multiBook.BookSummaries, x => Assert.True(x.CapitalShare > 0m));
        Assert.All(multiBook.PortfolioAllocations, x => Assert.False(string.IsNullOrWhiteSpace(x.StrategyBookId)));
    }

    [Fact]
    public void Engine_Run_Emits_PhaseTwo_Audit_Data()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();
        var run = new FundLaunchEngine().Run(scenario);

        Assert.Equal(2, run.StrategyBooks.Count);
        Assert.Contains(run.PolicyAudit, x => x.Status == "APPLIED");
        Assert.Contains(run.PolicyAudit, x => x.Status == "PENDING_APPROVAL");
        Assert.Contains(run.StrategyLifecycle, x => x.Hook == "INITIALIZE");
        Assert.Contains(run.StrategyLifecycle, x => x.Hook == "RUN_COMPLETED");
        Assert.All(run.ExecutionIntents, x => Assert.False(string.IsNullOrWhiteSpace(x.StrategyBookId)));
    }
}
