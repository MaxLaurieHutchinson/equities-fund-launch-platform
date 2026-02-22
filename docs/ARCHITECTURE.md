# Architecture

## System View

```mermaid
flowchart LR
    A["Strategy Registry"] --> B["Signal Aggregator"]
    B --> C["Capital Allocator"]
    C --> D["Risk Policy Gate"]
    D --> E["Execution Planner"]
    E --> J["Incident Simulator"]
    J --> K["Runtime Event Bus"]
    E --> F["Venue Adapter Layer"]
    E --> G["TCA Feedback Engine"]
    D --> H["Observability Control Plane"]
    J --> H
    E --> H
    G --> H
    H --> I["Audit & Dashboard Artifacts"]
    I --> L["Showcase Pack Writer (Public-Safe)"]
```

## Module Responsibilities

- `Strategy Registry`: strategy lifecycle, versioning, enable/disable flags.
- `Signal Aggregator`: normalized strategy outputs and confidence blending.
- `Capital Allocator`: per-book capital shares, exposure budgets, concentration limits, turnover shaping.
- `Risk Policy Gate`: hard limits and policy-based approvals.
- `Execution Planner`: order intent creation, route suggestion, urgency profile.
- `Incident Simulator`: regime selection, fault injection, replay frame generation.
- `Runtime Event Bus`: ordered event timeline for incident and operational traces.
- `TCA Feedback Engine`: slippage/cost attribution and policy feedback.
- `Feedback Loop Engine`: closed-loop route recommendations with guardrail decisions.
- `Observability Control Plane`: health checks, anomaly detection, incident timeline.
- `Showcase Pack Writer`: sanitizes runtime outputs for public-facing sharing.

## Runtime Contracts

- deterministic fixtures by default
- explicit artifact outputs (`md`, `csv`, `json`)
- policy override and approvals audit trail
- strategy plugin lifecycle trace (`initialize`, `composite-published`, `run-completed`)
- incident simulation traces (`timeline`, `replay`, `fault summary`)
- TCA + feedback artifacts (`fill quality`, `route summary`, `policy recommendations`)
- deterministic timestamp controls for reproducible snapshots
- no external secrets required for baseline runs

## Future Extensions

- multi-agent strategy coordination protocols
- systemic risk propagation simulator
- policy sandbox for intervention experiments
