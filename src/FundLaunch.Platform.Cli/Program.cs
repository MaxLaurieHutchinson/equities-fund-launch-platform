using FundLaunch.Platform.Core;

var scenario = FundLaunchScenarioFactory.CreateDeterministicScenario();
var engine = new FundLaunchEngine();

var run = engine.Run(scenario);
var summary = FundLaunchEngine.BuildSummary(run);

Console.WriteLine("Equities Fund Launch Platform - Project 13 kickoff slice");
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
    Console.WriteLine($"Telemetry JSON:          {outputDir}/telemetry-dashboard.json");
    Console.WriteLine($"Summary JSON:            {outputDir}/run-summary.json");
}
