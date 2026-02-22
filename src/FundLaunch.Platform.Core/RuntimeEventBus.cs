using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public interface IRuntimeEventBus
{
    void Publish(
        string eventType,
        string source,
        string detail,
        decimal impactScore,
        DateTime timestamp);

    IReadOnlyList<RuntimeEvent> Snapshot();
}

public sealed class InMemoryRuntimeEventBus : IRuntimeEventBus
{
    private readonly List<RuntimeEvent> _events = new();
    private int _sequence;

    public void Publish(
        string eventType,
        string source,
        string detail,
        decimal impactScore,
        DateTime timestamp)
    {
        _sequence++;

        _events.Add(new RuntimeEvent(
            Sequence: _sequence,
            Timestamp: timestamp,
            EventType: eventType,
            Source: source,
            Detail: detail,
            ImpactScore: Round6(impactScore)));
    }

    public IReadOnlyList<RuntimeEvent> Snapshot()
    {
        return _events
            .OrderBy(x => x.Sequence)
            .ToArray();
    }

    private static decimal Round6(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
