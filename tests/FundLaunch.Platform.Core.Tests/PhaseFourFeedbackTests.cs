using FundLaunch.Platform.Contracts;
using FundLaunch.Platform.Core;

namespace FundLaunch.Platform.Core.Tests;

public sealed class PhaseFourFeedbackTests
{
    [Fact]
    public void Engine_Run_Emits_Tca_And_Feedback_Data()
    {
        var run = new FundLaunchEngine().Run(FundLaunchScenarioFactory.CreateDeterministicScenario());
        var summary = FundLaunchEngine.BuildSummary(run);

        Assert.NotEmpty(run.TcaAnalysis.FillMetrics);
        Assert.NotEmpty(run.TcaAnalysis.RouteSummaries);
        Assert.True(run.TcaAnalysis.Summary.AvgFillRate >= 0m);
        Assert.True(run.TcaAnalysis.Summary.AvgSlippageBps >= 0m);
        Assert.NotEmpty(run.FeedbackLoop.Recommendations);
        Assert.NotEqual(string.Empty, run.FeedbackLoop.Summary.PolicyState);

        Assert.Equal(run.TcaAnalysis.Summary.AvgFillRate, summary.TcaAvgFillRate);
        Assert.Equal(run.TcaAnalysis.Summary.AvgSlippageBps, summary.TcaAvgSlippageBps);
        Assert.Equal(run.FeedbackLoop.Summary.RecommendationCount, summary.FeedbackRecommendationCount);
        Assert.Equal(run.FeedbackLoop.Summary.PolicyState, summary.FeedbackPolicyState);
    }

    [Fact]
    public void FeedbackLoop_Blocks_Changes_When_Risk_Is_Not_Approved()
    {
        var tca = new TcaAnalysisResult(
            FillMetrics:
            [
                new TcaFillMetric(
                    Symbol: "AAPL",
                    StrategyBookId: "BOOK_A",
                    Route: "LIT_SMART",
                    IntendedNotional: 120000m,
                    ExecutedNotional: 20000m,
                    FillRate: 0.166667m,
                    SlippageBps: 24.5m,
                    EstimatedCost: 49m,
                    QualityBand: "POOR")
            ],
            RouteSummaries:
            [
                new TcaRouteSummary(
                    Route: "LIT_SMART",
                    IntentCount: 1,
                    AvgFillRate: 0.166667m,
                    AvgSlippageBps: 24.5m,
                    TotalEstimatedCost: 49m,
                    PoorQualityCount: 1)
            ],
            Summary: new TcaSummary(
                AvgFillRate: 0.166667m,
                AvgSlippageBps: 24.5m,
                TotalEstimatedCost: 49m,
                PoorQualityCount: 1,
                BlockedIntentCount: 0));

        var incident = new IncidentSimulationResult(
            Regime: new MarketRegimeSnapshot("VOLATILE", 1.30m, 0.78m, 11.2m),
            Timeline: Array.Empty<RuntimeEvent>(),
            ActiveFaults: new[] { "VENUE_REJECT_BURST" },
            AdjustedIntents: Array.Empty<ExecutionIntent>(),
            ReplayFrames: Array.Empty<IncidentReplayFrame>(),
            RejectedNotional: 0m,
            AddedLatencyMs: 0m);

        var risk = new RiskDecision(
            Approved: false,
            Code: "REJECTED",
            Detail: "test",
            GrossExposure: 0m,
            NetExposure: 0m,
            Turnover: 0m,
            Breaches: new[] { "Turnover breach" });

        var result = FeedbackLoopEngine.BuildRecommendations(tca, risk, incident, DateTime.UtcNow);

        Assert.NotEmpty(result.Recommendations);
        Assert.All(result.Recommendations, x => Assert.Equal("BLOCKED", x.GuardrailDecision));
        Assert.Equal("GUARDRAILED_ONLY", result.Summary.PolicyState);
    }

    [Fact]
    public void TcaAnalyzer_Produces_Blocked_Quality_For_Rejected_Fills()
    {
        var baseline = new[]
        {
            new ExecutionIntent("AAPL", "BUY", 0.10m, 150000m, "LIT_SMART", "HIGH", "BOOK_A")
        };

        var adjusted = new[]
        {
            new ExecutionIntent("AAPL", "BUY", 0.10m, 0m, "REJECTED_BY_VENUE", "BLOCKED", "BOOK_A")
        };

        var incident = new IncidentSimulationResult(
            Regime: new MarketRegimeSnapshot("STRESS", 1.65m, 0.62m, 19.5m),
            Timeline: Array.Empty<RuntimeEvent>(),
            ActiveFaults: new[] { "VENUE_REJECT_BURST" },
            AdjustedIntents: adjusted,
            ReplayFrames: Array.Empty<IncidentReplayFrame>(),
            RejectedNotional: 150000m,
            AddedLatencyMs: 42m);

        var analysis = TcaAnalyzer.Analyze(baseline, adjusted, incident, DateTime.UtcNow);

        Assert.Single(analysis.FillMetrics);
        Assert.Equal("BLOCKED", analysis.FillMetrics[0].QualityBand);
        Assert.True(analysis.Summary.BlockedIntentCount >= 1);
    }
}
