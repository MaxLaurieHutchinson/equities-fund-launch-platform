# Equities Fund Launch Platform

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Stage](https://img.shields.io/badge/Stage-Project%2013%20Kickoff-0B7285)](docs/PROJECT13_PLAN.md)
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
- `Phase 1` in progress: first runnable vertical slice (strategy -> risk -> execution -> telemetry).

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

## Design Direction

- Architecture details: `docs/ARCHITECTURE.md`
- Delivery plan and milestones: `docs/PROJECT13_PLAN.md`
- Portfolio-facing one-page summary: `docs/SHOWCASE_ONE_PAGER.md`
