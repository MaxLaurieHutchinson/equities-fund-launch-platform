using FundLaunch.Platform.Contracts;
using FundLaunch.Platform.Core;

namespace FundLaunch.Platform.Core.Tests;

public sealed class PhaseThreeSimulationTests
{
    [Fact]
    public void IncidentSimulator_Builds_Timeline_And_Replay_From_Faults()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();
        var run = new FundLaunchEngine().Run(scenario);

        Assert.NotEmpty(run.IncidentSimulation.ActiveFaults);
        Assert.Contains("LATENCY_SPIKE", run.IncidentSimulation.ActiveFaults);
        Assert.Contains("VENUE_REJECT_BURST", run.IncidentSimulation.ActiveFaults);
        Assert.Contains("FEED_DROPOUT", run.IncidentSimulation.ActiveFaults);
        Assert.NotEmpty(run.IncidentSimulation.Timeline);
        Assert.Contains(run.IncidentSimulation.Timeline, x => x.EventType == "REGIME_SELECTED");
        Assert.Contains(run.IncidentSimulation.Timeline, x => x.EventType == "REPLAY_READY");
        Assert.Equal(run.IncidentSimulation.ReplayFrames.Count, run.ExecutionIntents.Count);
        Assert.True(run.IncidentSimulation.RejectedNotional >= 0m);
        Assert.True(run.IncidentSimulation.AddedLatencyMs > 0m);
    }

    [Fact]
    public void Engine_Can_Run_Without_Incident_Faults()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario() with
        {
            IncidentSimulation = new IncidentSimulationConfig(
                EnableLatencySpike: false,
                EnableVenueRejectBurst: false,
                EnableFeedDropout: false,
                LatencySpikeMultiplier: 1m,
                VenueRejectRatio: 0m,
                FeedDropoutRatio: 0m)
        };

        var run = new FundLaunchEngine().Run(scenario);
        var summary = FundLaunchEngine.BuildSummary(run);

        Assert.Empty(run.IncidentSimulation.ActiveFaults);
        Assert.True(summary.IncidentTimelineEvents >= 2);
        Assert.Equal(run.ExecutionIntents.Count, summary.IncidentReplayFrames);
        Assert.Equal(0, summary.ActiveIncidentFaults);
        Assert.Equal("RUNNING", run.Telemetry.ControlState);
    }

    [Fact]
    public void RuntimeEventBus_Assigns_Monotonic_Sequence()
    {
        var bus = new InMemoryRuntimeEventBus();
        var now = DateTime.UtcNow;

        bus.Publish("A", "S1", "first", 1m, now);
        bus.Publish("B", "S2", "second", 2m, now);
        var snapshot = bus.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Equal(1, snapshot[0].Sequence);
        Assert.Equal(2, snapshot[1].Sequence);
    }
}
