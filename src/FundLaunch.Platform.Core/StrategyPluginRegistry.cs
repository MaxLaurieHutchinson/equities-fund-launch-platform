using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public sealed record StrategyPluginContext(
    string StrategyId,
    DateTime Timestamp,
    string RunId);

public sealed record StrategyPluginHookResult(
    IReadOnlyList<StrategySignal> Signals,
    IReadOnlyList<StrategyPluginLifecycleEvent> Events);

public interface IStrategyPlugin
{
    string StrategyId { get; }

    IReadOnlyList<StrategySignal> OnInitialize(
        IReadOnlyList<StrategySignal> strategySignals,
        StrategyPluginContext context);

    void OnCompositePublished(
        IReadOnlyList<CompositeSignal> compositeSignals,
        StrategyPluginContext context);

    void OnRunCompleted(
        PlatformRunResult run,
        StrategyPluginContext context);
}

public sealed class StrategyPluginRegistry
{
    private readonly IReadOnlyDictionary<string, IStrategyPlugin> _plugins;

    public static StrategyPluginRegistry Empty { get; } = new(Array.Empty<IStrategyPlugin>());

    public StrategyPluginRegistry(IEnumerable<IStrategyPlugin> plugins)
    {
        _plugins = plugins
            .GroupBy(x => NormalizeKey(x.StrategyId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.Last(),
                StringComparer.OrdinalIgnoreCase);
    }

    public StrategyPluginHookResult ExecuteInitialize(
        IReadOnlyList<StrategySignal> inputSignals,
        DateTime timestamp,
        string runId)
    {
        var emittedSignals = new List<StrategySignal>(inputSignals.Count);
        var lifecycle = new List<StrategyPluginLifecycleEvent>();

        foreach (var group in inputSignals.GroupBy(x => NormalizeKey(x.StrategyId), StringComparer.OrdinalIgnoreCase))
        {
            var strategyId = group.Key;
            var groupedSignals = group.ToArray();

            if (!_plugins.TryGetValue(strategyId, out var plugin))
            {
                emittedSignals.AddRange(groupedSignals);
                continue;
            }

            var context = new StrategyPluginContext(strategyId, timestamp, runId);
            try
            {
                var transformed = plugin.OnInitialize(groupedSignals, context) ?? groupedSignals;
                emittedSignals.AddRange(transformed);

                lifecycle.Add(new StrategyPluginLifecycleEvent(
                    StrategyId: strategyId,
                    Hook: "INITIALIZE",
                    Status: "SUCCESS",
                    Detail: $"{transformed.Count} signals emitted.",
                    Timestamp: timestamp));
            }
            catch (Exception ex)
            {
                emittedSignals.AddRange(groupedSignals);

                lifecycle.Add(new StrategyPluginLifecycleEvent(
                    StrategyId: strategyId,
                    Hook: "INITIALIZE",
                    Status: "FAILED",
                    Detail: TrimDetail(ex.Message),
                    Timestamp: timestamp));
            }
        }

        return new StrategyPluginHookResult(
            Signals: emittedSignals,
            Events: lifecycle);
    }

    public IReadOnlyList<StrategyPluginLifecycleEvent> ExecuteCompositePublished(
        IReadOnlyList<CompositeSignal> compositeSignals,
        DateTime timestamp,
        string runId)
    {
        var lifecycle = new List<StrategyPluginLifecycleEvent>();

        foreach (var plugin in _plugins.Values.OrderBy(x => x.StrategyId, StringComparer.OrdinalIgnoreCase))
        {
            var strategyId = NormalizeKey(plugin.StrategyId);
            var context = new StrategyPluginContext(strategyId, timestamp, runId);
            try
            {
                plugin.OnCompositePublished(compositeSignals, context);

                lifecycle.Add(new StrategyPluginLifecycleEvent(
                    StrategyId: strategyId,
                    Hook: "COMPOSITE_PUBLISHED",
                    Status: "SUCCESS",
                    Detail: "Composite signals observed.",
                    Timestamp: timestamp));
            }
            catch (Exception ex)
            {
                lifecycle.Add(new StrategyPluginLifecycleEvent(
                    StrategyId: strategyId,
                    Hook: "COMPOSITE_PUBLISHED",
                    Status: "FAILED",
                    Detail: TrimDetail(ex.Message),
                    Timestamp: timestamp));
            }
        }

        return lifecycle;
    }

    public IReadOnlyList<StrategyPluginLifecycleEvent> ExecuteRunCompleted(
        PlatformRunResult run,
        DateTime timestamp,
        string runId)
    {
        var lifecycle = new List<StrategyPluginLifecycleEvent>();

        foreach (var plugin in _plugins.Values.OrderBy(x => x.StrategyId, StringComparer.OrdinalIgnoreCase))
        {
            var strategyId = NormalizeKey(plugin.StrategyId);
            var context = new StrategyPluginContext(strategyId, timestamp, runId);
            try
            {
                plugin.OnRunCompleted(run, context);

                lifecycle.Add(new StrategyPluginLifecycleEvent(
                    StrategyId: strategyId,
                    Hook: "RUN_COMPLETED",
                    Status: "SUCCESS",
                    Detail: "Run completion acknowledged.",
                    Timestamp: timestamp));
            }
            catch (Exception ex)
            {
                lifecycle.Add(new StrategyPluginLifecycleEvent(
                    StrategyId: strategyId,
                    Hook: "RUN_COMPLETED",
                    Status: "FAILED",
                    Detail: TrimDetail(ex.Message),
                    Timestamp: timestamp));
            }
        }

        return lifecycle;
    }

    private static string NormalizeKey(string strategyId)
    {
        return strategyId.Trim().ToUpperInvariant();
    }

    private static string TrimDetail(string message)
    {
        const int maxLength = 120;
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Plugin hook failed.";
        }

        return message.Length <= maxLength ? message : message[..maxLength];
    }
}

public sealed class NoOpStrategyPlugin(string strategyId) : IStrategyPlugin
{
    public string StrategyId { get; } = strategyId;

    public IReadOnlyList<StrategySignal> OnInitialize(
        IReadOnlyList<StrategySignal> strategySignals,
        StrategyPluginContext context)
    {
        return strategySignals;
    }

    public void OnCompositePublished(
        IReadOnlyList<CompositeSignal> compositeSignals,
        StrategyPluginContext context)
    {
    }

    public void OnRunCompleted(
        PlatformRunResult run,
        StrategyPluginContext context)
    {
    }
}

public sealed class ConfidenceFloorPlugin(string strategyId, decimal minConfidence) : IStrategyPlugin
{
    public string StrategyId { get; } = strategyId;

    public IReadOnlyList<StrategySignal> OnInitialize(
        IReadOnlyList<StrategySignal> strategySignals,
        StrategyPluginContext context)
    {
        return strategySignals
            .Select(x => x with { Confidence = Math.Max(minConfidence, x.Confidence) })
            .ToArray();
    }

    public void OnCompositePublished(
        IReadOnlyList<CompositeSignal> compositeSignals,
        StrategyPluginContext context)
    {
    }

    public void OnRunCompleted(
        PlatformRunResult run,
        StrategyPluginContext context)
    {
    }
}

public sealed class AlphaScalePlugin(string strategyId, decimal alphaScale) : IStrategyPlugin
{
    public string StrategyId { get; } = strategyId;

    public IReadOnlyList<StrategySignal> OnInitialize(
        IReadOnlyList<StrategySignal> strategySignals,
        StrategyPluginContext context)
    {
        return strategySignals
            .Select(x =>
            {
                var adjusted = decimal.Round(x.AlphaScore * alphaScale, 6, MidpointRounding.AwayFromZero);
                adjusted = Math.Max(-1m, Math.Min(1m, adjusted));
                return x with { AlphaScore = adjusted };
            })
            .ToArray();
    }

    public void OnCompositePublished(
        IReadOnlyList<CompositeSignal> compositeSignals,
        StrategyPluginContext context)
    {
    }

    public void OnRunCompleted(
        PlatformRunResult run,
        StrategyPluginContext context)
    {
    }
}

public static class StrategyPluginFactory
{
    public static StrategyPluginRegistry CreateDeterministicRegistry()
    {
        return new StrategyPluginRegistry(
        [
            new ConfidenceFloorPlugin("TREND_CORE", 0.70m),
            new AlphaScalePlugin("MEAN_REV", 0.92m),
            new NoOpStrategyPlugin("MACRO_REGIME"),
            new NoOpStrategyPlugin("QUALITY_LONG")
        ]);
    }
}
