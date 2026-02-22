using System.Globalization;
using System.Text;
using System.Text.Json;
using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public static class ArtifactWriter
{
    public static void Write(string outputDir, PlatformRunResult run)
    {
        Directory.CreateDirectory(outputDir);

        var summary = FundLaunchEngine.BuildSummary(run);

        var markdownPath = Path.Combine(outputDir, "latest-run-report.md");
        var intentsPath = Path.Combine(outputDir, "execution-intents.csv");
        var allocationsPath = Path.Combine(outputDir, "allocations.csv");
        var telemetryPath = Path.Combine(outputDir, "telemetry-dashboard.json");
        var summaryPath = Path.Combine(outputDir, "run-summary.json");

        File.WriteAllText(markdownPath, BuildMarkdown(summary));
        File.WriteAllText(intentsPath, BuildIntentCsv(run.ExecutionIntents));
        File.WriteAllText(allocationsPath, BuildAllocationCsv(run.Allocations));
        File.WriteAllText(telemetryPath, JsonSerializer.Serialize(run.Telemetry, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string BuildMarkdown(PlatformRunSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Equities Fund Launch Platform Run Report");
        sb.AppendLine();
        sb.AppendLine($"- Signal symbols: `{summary.SignalSymbolCount}`");
        sb.AppendLine($"- Allocations: `{summary.AllocationCount}`");
        sb.AppendLine($"- Risk approved: `{summary.RiskApproved}`");
        sb.AppendLine($"- Breach count: `{summary.BreachCount}`");
        sb.AppendLine($"- Execution intents: `{summary.ExecutionIntentCount}`");
        sb.AppendLine($"- Gross exposure: `{summary.GrossExposure:F4}`");
        sb.AppendLine($"- Net exposure: `{summary.NetExposure:F4}`");
        sb.AppendLine($"- Turnover: `{summary.Turnover:F4}`");
        sb.AppendLine($"- Total execution notional: `{summary.TotalExecutionNotional:F2}`");
        sb.AppendLine($"- Top signal: `{summary.TopSignalSymbol}` (`{summary.TopSignalScore:F4}`)");
        sb.AppendLine($"- Fleet health score: `{summary.FleetHealthScore:F2}`");
        sb.AppendLine($"- Control state: `{summary.ControlState}`");

        return sb.ToString();
    }

    private static string BuildIntentCsv(IReadOnlyList<ExecutionIntent> intents)
    {
        var sb = new StringBuilder();
        sb.AppendLine("symbol,side,delta_weight,notional,route,urgency");

        foreach (var intent in intents)
        {
            sb.AppendLine(string.Join(',',
                intent.Symbol,
                intent.Side,
                intent.DeltaWeight.ToString("F6", CultureInfo.InvariantCulture),
                intent.Notional.ToString("F2", CultureInfo.InvariantCulture),
                intent.Route,
                intent.Urgency));
        }

        return sb.ToString();
    }

    private static string BuildAllocationCsv(IReadOnlyList<AllocationDraft> allocations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("symbol,current_weight,target_weight,delta_weight,action,rationale");

        foreach (var allocation in allocations)
        {
            sb.AppendLine(string.Join(',',
                allocation.Symbol,
                allocation.CurrentWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.TargetWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.DeltaWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.Action,
                allocation.Rationale.Replace(',', ';')));
        }

        return sb.ToString();
    }
}
