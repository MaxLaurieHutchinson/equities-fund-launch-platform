# Equities Fund Launch Platform

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Stage](https://img.shields.io/badge/Stage-Phase%202%20Runtime-0B7285)](docs/PROJECT13_PLAN.md)
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

## Repository Map

```text
.
├── docs/
│   ├── ARCHITECTURE.md
│   ├── PROJECT13_PLAN.md
│   └── SHOWCASE_ONE_PAGER.md
├── src/
├── tests/
└── README.md
```

## Quick Start

```bash
# (after initial code bootstrap)
dotnet test
dotnet run --project src/FundLaunch.Platform.Cli -- reports
```

`reports/` now includes:
- `latest-run-report.md`
- `execution-intents.csv`
- `allocations.csv`
- `strategy-books.csv`
- `policy-override-audit.csv`
- `strategy-plugin-lifecycle.csv`
- `telemetry-dashboard.json`
- `run-summary.json`

## Design Direction

- Architecture details: `docs/ARCHITECTURE.md`
- Delivery plan and milestones: `docs/PROJECT13_PLAN.md`
- Portfolio-facing one-page summary: `docs/SHOWCASE_ONE_PAGER.md`

## Safety Guardrails

This clone is configured for public-safe development and PR-first workflow.

```bash
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit
chmod +x .githooks/pre-push
```

- `pre-commit`: blocks private/sensitive file patterns.
- `pre-push`: blocks direct pushes to `main`/`master` (branch + PR flow).
