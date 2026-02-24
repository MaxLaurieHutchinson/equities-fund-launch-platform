using FundLaunch.Platform.Contracts;
using FundLaunch.Platform.Core;

namespace FundLaunch.Platform.Core.Tests;

public sealed class PhaseSixAgentArenaTests
{
    [Fact]
    public void Engine_Run_Emits_AgentArena_Result()
    {
        var run = new FundLaunchEngine().Run(FundLaunchScenarioFactory.CreateDeterministicScenario());
        var summary = FundLaunchEngine.BuildSummary(run);

        Assert.True(run.AgentArena.Summary.Enabled);
        Assert.True(run.AgentArena.Summary.RoundsExecuted >= 1);
        Assert.True(run.AgentArena.Summary.ParticipatingAgents >= 1);
        Assert.NotEmpty(run.AgentArena.Bids);
        Assert.NotEmpty(run.AgentArena.Outcomes);

        var finalShareTotal = run.AgentArena.Outcomes.Sum(x => x.FinalCapitalShare);
        Assert.InRange(finalShareTotal, 0.9999m, 1.0001m);

        Assert.Equal(run.AgentArena.Summary.RoundsExecuted, summary.AgentArenaRounds);
        Assert.Equal(run.AgentArena.Summary.ParticipatingAgents, summary.AgentArenaAgents);
        Assert.Equal(run.AgentArena.Summary.PolicyState, summary.AgentArenaPolicyState);
    }

    [Fact]
    public void AgentArena_Disabled_Config_Returns_Disabled_State()
    {
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario() with
        {
            AgentArena = new AgentArenaConfig(
                Enabled: false,
                NegotiationRounds: 3,
                MaxShiftPerRound: 0.05m,
                MinConvergenceScore: 0.85m)
        };

        var run = new FundLaunchEngine().Run(scenario);

        Assert.False(run.AgentArena.Summary.Enabled);
        Assert.Equal("DISABLED", run.AgentArena.Summary.PolicyState);
        Assert.Empty(run.AgentArena.Bids);
    }

    [Fact]
    public void AgentArena_Is_Deterministic_With_FixedTimestamp()
    {
        var fixedTs = new DateTime(2026, 02, 22, 12, 00, 00, DateTimeKind.Utc);
        var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario() with
        {
            FixedTimestampUtc = fixedTs
        };

        var engine = new FundLaunchEngine();
        var a = engine.Run(scenario);
        var b = engine.Run(scenario);

        Assert.Equal(
            a.AgentArena.Bids.Select(x => (x.Round, x.AgentId, x.GrantedCapitalShare, x.Decision)),
            b.AgentArena.Bids.Select(x => (x.Round, x.AgentId, x.GrantedCapitalShare, x.Decision)));
    }
}
