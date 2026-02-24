using FundLaunch.Platform.Core;

namespace FundLaunch.Platform.Core.Tests;

public sealed class PhaseFiveShowcaseTests
{
    [Fact]
    public void Engine_Uses_FixedTimestamp_For_Deterministic_Run()
    {
        var timestamp = new DateTime(2026, 02, 22, 12, 00, 00, DateTimeKind.Utc);
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario() with
        {
            FixedTimestampUtc = timestamp
        };

        var engine = new FundLaunchEngine();
        var runA = engine.Run(scenario);
        var runB = engine.Run(scenario);

        Assert.Equal(timestamp, runA.Timestamp);
        Assert.Equal(timestamp, runB.Timestamp);
        Assert.Equal(runA.ExecutionIntents.Count, runB.ExecutionIntents.Count);
        Assert.Equal(
            runA.IncidentSimulation.Timeline.Select(x => x.Timestamp),
            runB.IncidentSimulation.Timeline.Select(x => x.Timestamp));
    }

    [Fact]
    public void ShowcasePackWriter_Writes_Sanitized_Files()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();
        var run = new FundLaunchEngine().Run(scenario);
        var outputDir = Path.Combine(Path.GetTempPath(), "fund-launch-showcase-tests", Guid.NewGuid().ToString("N"));

        try
        {
            ShowcasePackWriter.WritePublicSnapshot(outputDir, run);

            var publicReportPath = Path.Combine(outputDir, "public-run-report.md");
            var publicSummaryPath = Path.Combine(outputDir, "public-run-summary.json");
            var publicIntentsPath = Path.Combine(outputDir, "public-execution-intents.csv");
            var publicFeedbackPath = Path.Combine(outputDir, "public-feedback-recommendations.csv");
            var publicTimelinePath = Path.Combine(outputDir, "public-event-timeline.csv");
            var publicLifecyclePath = Path.Combine(outputDir, "public-strategy-lifecycle.csv");
            var publicArenaPath = Path.Combine(outputDir, "public-agent-arena-bids.csv");

            Assert.True(File.Exists(publicReportPath));
            Assert.True(File.Exists(publicSummaryPath));
            Assert.True(File.Exists(publicIntentsPath));
            Assert.True(File.Exists(publicFeedbackPath));
            Assert.True(File.Exists(publicTimelinePath));
            Assert.True(File.Exists(publicLifecyclePath));
            Assert.True(File.Exists(publicArenaPath));

            var intentsText = File.ReadAllText(publicIntentsPath);
            var lifecycleText = File.ReadAllText(publicLifecyclePath);
            var arenaText = File.ReadAllText(publicArenaPath);

            Assert.Contains("EQ01", intentsText);
            Assert.DoesNotContain("AAPL", intentsText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MSFT", intentsText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TREND_CORE", lifecycleText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MEAN_REV", lifecycleText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("STRAT01", lifecycleText);
            Assert.Contains("BOOK01", arenaText);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }
}
