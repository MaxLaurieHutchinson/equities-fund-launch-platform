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
        var booksPath = Path.Combine(outputDir, "strategy-books.csv");
        var policyPath = Path.Combine(outputDir, "policy-override-audit.csv");
        var lifecyclePath = Path.Combine(outputDir, "strategy-plugin-lifecycle.csv");
        var telemetryPath = Path.Combine(outputDir, "telemetry-dashboard.json");
        var summaryPath = Path.Combine(outputDir, "run-summary.json");

        File.WriteAllText(markdownPath, BuildMarkdown(summary));
        File.WriteAllText(intentsPath, BuildIntentCsv(run.ExecutionIntents));
        File.WriteAllText(allocationsPath, BuildAllocationCsv(run.Allocations));
        File.WriteAllText(booksPath, BuildStrategyBookCsv(run.StrategyBooks));
        File.WriteAllText(policyPath, BuildPolicyAuditCsv(run.PolicyAudit));
        File.WriteAllText(lifecyclePath, BuildStrategyLifecycleCsv(run.StrategyLifecycle));
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
        sb.AppendLine($"- Strategy books: `{summary.StrategyBookCount}`");
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
        sb.AppendLine($"- Applied policy overrides: `{summary.AppliedPolicyOverrideCount}`");
        sb.AppendLine($"- Pending policy overrides: `{summary.PendingPolicyOverrideCount}`");
        sb.AppendLine($"- Strategy lifecycle events: `{summary.StrategyLifecycleEvents}`");

        return sb.ToString();
    }

    private static string BuildIntentCsv(IReadOnlyList<ExecutionIntent> intents)
    {
        var sb = new StringBuilder();
        sb.AppendLine("symbol,side,delta_weight,notional,route,urgency,strategy_book_id");

        foreach (var intent in intents)
        {
            sb.AppendLine(string.Join(',',
                intent.Symbol,
                intent.Side,
                intent.DeltaWeight.ToString("F6", CultureInfo.InvariantCulture),
                intent.Notional.ToString("F2", CultureInfo.InvariantCulture),
                intent.Route,
                intent.Urgency,
                intent.StrategyBookId));
        }

        return sb.ToString();
    }

    private static string BuildAllocationCsv(IReadOnlyList<AllocationDraft> allocations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("symbol,current_weight,target_weight,delta_weight,action,rationale,strategy_book_id");

        foreach (var allocation in allocations)
        {
            sb.AppendLine(string.Join(',',
                allocation.Symbol,
                allocation.CurrentWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.TargetWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.DeltaWeight.ToString("F6", CultureInfo.InvariantCulture),
                allocation.Action,
                allocation.Rationale.Replace(',', ';'),
                allocation.StrategyBookId));
        }

        return sb.ToString();
    }

    private static string BuildStrategyBookCsv(IReadOnlyList<StrategyBookAllocationSummary> strategyBooks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("book_id,capital_share,allocation_count,gross_exposure,net_exposure,turnover");

        foreach (var book in strategyBooks)
        {
            sb.AppendLine(string.Join(',',
                book.BookId,
                book.CapitalShare.ToString("F6", CultureInfo.InvariantCulture),
                book.AllocationCount,
                book.GrossExposure.ToString("F6", CultureInfo.InvariantCulture),
                book.NetExposure.ToString("F6", CultureInfo.InvariantCulture),
                book.Turnover.ToString("F6", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildPolicyAuditCsv(IReadOnlyList<PolicyOverrideAuditEntry> auditTrail)
    {
        var sb = new StringBuilder();
        sb.AppendLine("policy_key,requested_value,prior_value,applied_value,status,reason,requested_by,approved_by,requested_at_utc,approved_at_utc,evaluated_at_utc");

        foreach (var entry in auditTrail)
        {
            sb.AppendLine(string.Join(',',
                entry.PolicyKey,
                entry.RequestedValue.ToString("F6", CultureInfo.InvariantCulture),
                entry.PriorValue?.ToString("F6", CultureInfo.InvariantCulture) ?? string.Empty,
                entry.AppliedValue?.ToString("F6", CultureInfo.InvariantCulture) ?? string.Empty,
                entry.Status,
                entry.Reason.Replace(',', ';'),
                entry.RequestedBy,
                entry.ApprovedBy ?? string.Empty,
                entry.RequestedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                entry.ApprovedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                entry.EvaluatedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildStrategyLifecycleCsv(IReadOnlyList<StrategyPluginLifecycleEvent> lifecycle)
    {
        var sb = new StringBuilder();
        sb.AppendLine("strategy_id,hook,status,detail,timestamp_utc");

        foreach (var entry in lifecycle)
        {
            sb.AppendLine(string.Join(',',
                entry.StrategyId,
                entry.Hook,
                entry.Status,
                entry.Detail.Replace(',', ';'),
                entry.Timestamp.ToString("O", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }
}
