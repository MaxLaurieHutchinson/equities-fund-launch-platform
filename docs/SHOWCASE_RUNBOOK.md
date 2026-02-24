# Showcase Runbook

This runbook creates a deterministic runtime snapshot and a public-safe showcase package.

## Prerequisites

- .NET 10 SDK installed
- repository cloned with hooks enabled

## One-Command Flow

Run from repository root:

```bash
./scripts/generate_showcase.sh
```

Optional custom deterministic timestamp:

```bash
./scripts/generate_showcase.sh 2026-03-01T10:30:00Z
```

## Outputs

Internal runtime artifacts:

- `reports/latest-run-report.md`
- `reports/execution-intents.csv`
- `reports/allocations.csv`
- `reports/strategy-books.csv`
- `reports/policy-override-audit.csv`
- `reports/strategy-plugin-lifecycle.csv`
- `reports/incident-event-timeline.csv`
- `reports/incident-replay.csv`
- `reports/incident-summary.json`
- `reports/tca-fill-quality.csv`
- `reports/tca-route-summary.csv`
- `reports/feedback-recommendations.csv`
- `reports/feedback-loop-summary.json`
- `reports/agent-arena-bids.csv`
- `reports/agent-arena-outcomes.csv`
- `reports/agent-arena-summary.json`
- `reports/telemetry-dashboard.json`
- `reports/run-summary.json`

Public-safe showcase package:

- `artifacts/showcase/public/public-run-report.md`
- `artifacts/showcase/public/public-run-summary.json`
- `artifacts/showcase/public/public-execution-intents.csv`
- `artifacts/showcase/public/public-feedback-recommendations.csv`
- `artifacts/showcase/public/public-event-timeline.csv`
- `artifacts/showcase/public/public-strategy-lifecycle.csv`
- `artifacts/showcase/public/public-agent-arena-bids.csv`

## Reproducibility Notes

- Runtime timestamp is fixed by default through the deterministic scenario factory.
- You can override with `--fixed-ts=<ISO-8601 UTC>` when running CLI directly.
- `scripts/generate_showcase.sh` always drives a deterministic timestamp.

## VS Code Quick Run

From VS Code terminal:

```bash
dotnet test
dotnet run --project src/FundLaunch.Platform.Cli -- reports showcase --fixed-ts=2026-02-22T12:00:00Z
```
