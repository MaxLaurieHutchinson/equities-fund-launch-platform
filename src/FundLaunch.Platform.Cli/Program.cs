using System.Globalization;
using FundLaunch.Platform.Core;

var fixedTimestampArg = args
    .FirstOrDefault(x => x.StartsWith("--fixed-ts=", StringComparison.OrdinalIgnoreCase));

DateTime? fixedTimestampUtc = null;
if (!string.IsNullOrWhiteSpace(fixedTimestampArg))
{
    var rawValue = fixedTimestampArg["--fixed-ts=".Length..];
    if (!DateTime.TryParse(
            rawValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed))
    {
        Console.Error.WriteLine($"Invalid fixed timestamp: {rawValue}");
        Console.Error.WriteLine("Expected ISO-8601, e.g. --fixed-ts=2026-02-22T12:00:00Z");
        return;
    }

    fixedTimestampUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
}

var baseScenario = FundLaunchScenarioFactory.CreateDeterministicScenario();
var scenario = baseScenario with
{
    FixedTimestampUtc = fixedTimestampUtc ?? baseScenario.FixedTimestampUtc
};
var engine = new FundLaunchEngine();

var run = engine.Run(scenario);
var summary = FundLaunchEngine.BuildSummary(run);

Console.WriteLine("Equities Fund Launch Platform - Project 13 integrated runtime");
Console.WriteLine($"Signal symbols:          {summary.SignalSymbolCount}");
Console.WriteLine($"Allocations:             {summary.AllocationCount}");
Console.WriteLine($"Strategy books:          {summary.StrategyBookCount}");
Console.WriteLine($"Risk approved:           {summary.RiskApproved}");
Console.WriteLine($"Breaches:                {summary.BreachCount}");
Console.WriteLine($"Execution intents:       {summary.ExecutionIntentCount}");
Console.WriteLine($"Gross exposure:          {summary.GrossExposure:F4}");
Console.WriteLine($"Net exposure:            {summary.NetExposure:F4}");
Console.WriteLine($"Turnover:                {summary.Turnover:F4}");
Console.WriteLine($"Execution notional:      {summary.TotalExecutionNotional:F2}");
Console.WriteLine($"Top signal:              {summary.TopSignalSymbol} ({summary.TopSignalScore:F4})");
Console.WriteLine($"Fleet health score:      {summary.FleetHealthScore:F2}");
Console.WriteLine($"Control state:           {summary.ControlState}");
Console.WriteLine($"Policy overrides(applied/pending): {summary.AppliedPolicyOverrideCount}/{summary.PendingPolicyOverrideCount}");
Console.WriteLine($"Plugin lifecycle events: {summary.StrategyLifecycleEvents}");
Console.WriteLine($"Incident events:         {summary.IncidentTimelineEvents}");
Console.WriteLine($"Incident replay frames:  {summary.IncidentReplayFrames}");
Console.WriteLine($"Active incident faults:  {summary.ActiveIncidentFaults}");
Console.WriteLine($"TCA avg fill rate:       {summary.TcaAvgFillRate:F4}");
Console.WriteLine($"TCA avg slippage (bps):  {summary.TcaAvgSlippageBps:F2}");
Console.WriteLine($"TCA est. cost:           {summary.TcaTotalEstimatedCost:F2}");
Console.WriteLine($"Feedback recs:           {summary.FeedbackRecommendationCount}");
Console.WriteLine($"Feedback approved/blocked: {summary.FeedbackApprovedCount}/{summary.FeedbackBlockedCount}");
Console.WriteLine($"Feedback policy state:   {summary.FeedbackPolicyState}");
Console.WriteLine($"Run timestamp (UTC):     {run.Timestamp:O}");

if (args.Contains("reports", StringComparer.OrdinalIgnoreCase))
{
    var outputDir = "reports";
    ArtifactWriter.Write(outputDir, run);

    Console.WriteLine();
    Console.WriteLine($"Report written:          {outputDir}/latest-run-report.md");
    Console.WriteLine($"Intents CSV:             {outputDir}/execution-intents.csv");
    Console.WriteLine($"Allocations CSV:         {outputDir}/allocations.csv");
    Console.WriteLine($"Strategy books CSV:      {outputDir}/strategy-books.csv");
    Console.WriteLine($"Policy audit CSV:        {outputDir}/policy-override-audit.csv");
    Console.WriteLine($"Plugin lifecycle CSV:    {outputDir}/strategy-plugin-lifecycle.csv");
    Console.WriteLine($"Incident timeline CSV:   {outputDir}/incident-event-timeline.csv");
    Console.WriteLine($"Incident replay CSV:     {outputDir}/incident-replay.csv");
    Console.WriteLine($"Incident summary JSON:   {outputDir}/incident-summary.json");
    Console.WriteLine($"TCA fill CSV:            {outputDir}/tca-fill-quality.csv");
    Console.WriteLine($"TCA route CSV:           {outputDir}/tca-route-summary.csv");
    Console.WriteLine($"Feedback recs CSV:       {outputDir}/feedback-recommendations.csv");
    Console.WriteLine($"Feedback summary JSON:   {outputDir}/feedback-loop-summary.json");
    Console.WriteLine($"Telemetry JSON:          {outputDir}/telemetry-dashboard.json");
    Console.WriteLine($"Summary JSON:            {outputDir}/run-summary.json");
}

if (args.Contains("showcase", StringComparer.OrdinalIgnoreCase))
{
    var outputDir = Path.Combine("artifacts", "showcase", "public");
    ShowcasePackWriter.WritePublicSnapshot(outputDir, run);

    Console.WriteLine();
    Console.WriteLine($"Showcase report:         {outputDir}/public-run-report.md");
    Console.WriteLine($"Showcase summary:        {outputDir}/public-run-summary.json");
    Console.WriteLine($"Showcase intents:        {outputDir}/public-execution-intents.csv");
    Console.WriteLine($"Showcase feedback:       {outputDir}/public-feedback-recommendations.csv");
    Console.WriteLine($"Showcase timeline:       {outputDir}/public-event-timeline.csv");
    Console.WriteLine($"Showcase lifecycle:      {outputDir}/public-strategy-lifecycle.csv");
}
