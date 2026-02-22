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

## Phase 1 - Vertical Slice (In Progress)
- Build end-to-end deterministic path:
  - strategy signals -> allocation draft -> risk gate -> execution intents -> telemetry snapshot
- Output artifacts:
  - run report (`md`)
  - execution intents (`csv`)
  - dashboard payload (`json`)
- Add unit tests around core path.

## Phase 2 - Multi-Strategy Runtime
- Strategy plugin registry with lifecycle hooks.
- Capital allocator supporting multiple strategy books.
- Policy overrides and approvals audit.

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

### Milestone M1 (current target)
- runnable CLI with deterministic data
- at least 3 modules wired (signals, risk, execution)
- tests passing
- artifact generation enabled

## Stretch Targets

- Add `agent arena` mode where multiple strategy agents negotiate for capital.
- Add systemic-risk mini-lab (cascade replay + containment response).
