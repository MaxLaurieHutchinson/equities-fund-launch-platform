# Project 13 Plan - Equities Fund Launch Platform

## Mission
Build an integrated C# platform that can stand up a new algorithmic equities sub-fund from technology perspective: strategy enablement, risk controls, execution workflows, and operational oversight.

## Fit Targets

- **Target technology-lead fit:** technology ownership + hands-on C# + PM/trader enablement.
- **Research extension path:** multi-agent coordination and collective behavior can be layered on top after core runtime stabilizes.

## Delivery Phases

## Phase 0 - Kickoff (Done)
- Separate repository created.
- Architecture and roadmap documented.
- Portfolio-facing one-pager added.

## Phase 1 - Vertical Slice (Done)
- Build end-to-end deterministic path:
  - strategy signals -> allocation draft -> risk gate -> execution intents -> telemetry snapshot
- Output artifacts:
  - run report (`md`)
  - execution intents (`csv`)
  - dashboard payload (`json`)
- Add unit tests around core path.

## Phase 2 - Multi-Strategy Runtime (Done)
- Strategy plugin registry with lifecycle hooks (`initialize`, `composite-published`, `run-completed`).
- Capital allocator supporting multiple strategy books with portfolio roll-up.
- Policy overrides with approval-state audit (`applied`, `pending`, `expired`, `unsupported`).
- New artifact outputs:
  - strategy book exposure pack (`strategy-books.csv`)
  - policy override audit (`policy-override-audit.csv`)
  - plugin lifecycle timeline (`strategy-plugin-lifecycle.csv`)

## Phase 3 - Market + Incident Simulation (Done)
- Market regime simulator and in-memory runtime event bus.
- Fault injection scenarios:
  - latency spike
  - venue reject burst
  - feed dropout
- Replay + post-incident artifact pack:
  - incident event timeline (`incident-event-timeline.csv`)
  - execution replay (`incident-replay.csv`)
  - incident summary payload (`incident-summary.json`)

## Phase 4 - TCA + Feedback Loops (Done)
- Fill-quality analytics integrated with routing policy.
- Route-level TCA summaries and estimated execution cost metrics.
- Closed-loop tuning recommendations with safety guardrails (`approved`, `blocked`, `monitor`).
- New artifact outputs:
  - TCA fill metrics (`tca-fill-quality.csv`)
  - TCA route summary (`tca-route-summary.csv`)
  - feedback recommendations (`feedback-recommendations.csv`)
  - feedback loop summary (`feedback-loop-summary.json`)

## Phase 5 - Showcase Hardening (Done)
- Clear module docs and architecture diagrams (`docs/MODULE_GUIDE.md`).
- Deterministic demo script and reproducible runbook (`scripts/generate_showcase.sh`, `docs/SHOWCASE_RUNBOOK.md`).
- Public-safe outputs and sanitized package flow (`ShowcasePackWriter` -> `artifacts/showcase/public`).

## Initial Milestone Definition

### Milestone M5 (current target)
- keep full runtime deterministic and test-stable
- maintain public-safe packaging boundary
- prepare optional expansion tracks (agent arena, systemic risk mini-lab)

## Stretch Targets

- Add `agent arena` mode where multiple strategy agents negotiate for capital.
- Add systemic-risk mini-lab (cascade replay + containment response).
