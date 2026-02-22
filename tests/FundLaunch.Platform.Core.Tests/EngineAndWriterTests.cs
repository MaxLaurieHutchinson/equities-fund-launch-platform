using FundLaunch.Platform.Core;

namespace FundLaunch.Platform.Core.Tests;

public sealed class EngineAndWriterTests
{
    [Fact]
    public void Engine_Run_Produces_Valid_Summary()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();
        var engine = new FundLaunchEngine();

        var run = engine.Run(scenario);
        var summary = FundLaunchEngine.BuildSummary(run);

        Assert.True(summary.SignalSymbolCount >= 4);
        Assert.True(summary.AllocationCount >= 4);
        Assert.True(summary.TotalExecutionNotional >= 0m);
        Assert.NotEqual("(none)", summary.TopSignalSymbol);
        Assert.NotEqual(string.Empty, summary.ControlState);
    }

    [Fact]
    public void ArtifactWriter_Writes_Expected_Files()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();
        var run = new FundLaunchEngine().Run(scenario);
        var outputDir = Path.Combine(Path.GetTempPath(), "fund-launch-platform-tests", Guid.NewGuid().ToString("N"));

        try
        {
            ArtifactWriter.Write(outputDir, run);

            Assert.True(File.Exists(Path.Combine(outputDir, "latest-run-report.md")));
            Assert.True(File.Exists(Path.Combine(outputDir, "execution-intents.csv")));
            Assert.True(File.Exists(Path.Combine(outputDir, "allocations.csv")));
            Assert.True(File.Exists(Path.Combine(outputDir, "telemetry-dashboard.json")));
            Assert.True(File.Exists(Path.Combine(outputDir, "run-summary.json")));
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
