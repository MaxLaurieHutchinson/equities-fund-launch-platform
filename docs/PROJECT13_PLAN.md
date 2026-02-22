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

## Phase 3 - Market + Incident Simulation
- Market regime simulator and event bus.
- Fault injection scenarios (latency spike, venue reject burst, feed dropout).
- Replay + post-incident artifact pack.

## Phase 4 - TCA + Feedback Loops
- Fill-quality analytics integrated with routing policy.
- Closed-loop tuning recommendations with safety guardrails.

## Phase 5 - Showcase Hardening
- Clear module docs and architecture diagrams.
- Deterministic demo script and reproducible runbook.
- Portfolio-safe outputs and sanitized fixtures.

## Initial Milestone Definition

### Milestone M2 (current target)
- keep deterministic runtime stable while adding simulation/event features
- preserve policy/audit and strategy-lifecycle traces in all scenarios
- maintain full green test suite for core runtime

## Stretch Targets

- Add `agent arena` mode where multiple strategy agents negotiate for capital.
- Add systemic-risk mini-lab (cascade replay + containment response).
