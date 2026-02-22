# Equities Fund Launch Platform

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Stage](https://img.shields.io/badge/Stage-Phase%205%20Showcase-0B7285)](docs/PROJECT13_PLAN.md)
[![Architecture](https://img.shields.io/badge/Architecture-Modular%20Control%20Plane-1C7ED6)](docs/ARCHITECTURE.md)

A C# flagship build for algorithmic fund-launch technology ownership.

This project composes strategy onboarding, PM/trader controls, risk gating, execution orchestration, TCA feedback, and observability into one integrated runtime.

## Why This Project

- Demonstrates hands-on C# leadership in a quant-fund environment.
- Shows system ownership across front-office and platform concerns.
- Provides a credible bridge from PoC labs into integrated production-style architecture.

## Build Goals

- Integrate the core ideas from Labs 05-12 into one coordinated platform.
- Keep deterministic fixtures and auditable outputs (`md`, `csv`, `json`).
- Add realistic operating concerns: incidents, policy changes, and replayable scenarios.

## Current Status

- `Phase 0` kickoff complete: repo scaffold + architecture + implementation plan.
- `Phase 1` complete: deterministic vertical slice (strategy -> risk -> execution -> telemetry).
- `Phase 2` complete: plugin lifecycle hooks, multi-book allocator, and policy override audit trail.
- `Phase 3` complete: market regime simulation, incident fault injection, replay/timeline artifacts.
- `Phase 4` complete: TCA fill-quality analytics and closed-loop feedback recommendations with guardrails.
- `Phase 5` complete: deterministic showcase runbook, module guide, and public-safe packaging flow.

## Repository Map

```text
.
├── docs/
│   ├── ARCHITECTURE.md
│   ├── MODULE_GUIDE.md
│   ├── PROJECT13_PLAN.md
│   ├── SHOWCASE_ONE_PAGER.md
│   └── SHOWCASE_RUNBOOK.md
├── scripts/
│   └── generate_showcase.sh
├── src/
├── tests/
└── README.md
```

## Quick Start

```bash
# (after initial code bootstrap)
dotnet test
dotnet run --project src/FundLaunch.Platform.Cli -- reports
./scripts/generate_showcase.sh
```

`reports/` now includes:
- `latest-run-report.md`
- `execution-intents.csv`
- `allocations.csv`
- `strategy-books.csv`
- `policy-override-audit.csv`
- `strategy-plugin-lifecycle.csv`
- `incident-event-timeline.csv`
- `incident-replay.csv`
- `incident-summary.json`
- `tca-fill-quality.csv`
- `tca-route-summary.csv`
- `feedback-recommendations.csv`
- `feedback-loop-summary.json`
- `telemetry-dashboard.json`
- `run-summary.json`

Public-safe showcase pack (sanitized aliases) is written to:
- `artifacts/showcase/public/public-run-report.md`
- `artifacts/showcase/public/public-run-summary.json`
- `artifacts/showcase/public/public-execution-intents.csv`
- `artifacts/showcase/public/public-feedback-recommendations.csv`
- `artifacts/showcase/public/public-event-timeline.csv`
- `artifacts/showcase/public/public-strategy-lifecycle.csv`

## Design Direction

- Architecture details: `docs/ARCHITECTURE.md`
- Module responsibilities: `docs/MODULE_GUIDE.md`
- Delivery plan and milestones: `docs/PROJECT13_PLAN.md`
- Portfolio-facing one-page summary: `docs/SHOWCASE_ONE_PAGER.md`
- Showcase reproduction steps: `docs/SHOWCASE_RUNBOOK.md`

## Safety Guardrails

This clone is configured for public-safe development and PR-first workflow.

```bash
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit
chmod +x .githooks/pre-push
```

- `pre-commit`: blocks private/sensitive file patterns.
- `pre-push`: blocks direct pushes to `main`/`master` (branch + PR flow).
