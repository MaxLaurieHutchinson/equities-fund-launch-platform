# Best Practices

This repository is a learning demo for financial systems architecture.
These practices keep it reproducible, auditable, and safe to extend.

## 1. Reproducibility First

- Keep deterministic fixtures as the default path.
- Use fixed UTC timestamps for repeatable artifacts.
- Treat reproducibility as a contract, not a convenience.

## 2. Artifact-Driven Validation

- Every meaningful runtime path should write explicit outputs.
- Prefer machine-readable artifacts (`csv`, `json`) plus human-readable summary (`md`).
- Keep artifact names stable so comparisons across runs are straightforward.

## 3. Test by Phase

- Add tests with each capability phase to avoid silent regressions.
- Preserve a stable deterministic baseline test path before adding new behavior.
- Validate both business outcomes and artifact shape contracts.

## 4. Risk and Policy as Code

- Enforce risk limits through explicit gate modules, not hidden conditions.
- Keep policy override behavior auditable (`applied`, `pending`, `expired`, `unsupported`).
- Persist policy decisions in artifacts so reviewers can trace why a run passed or failed.

## 5. Runtime Observability by Default

- Emit timeline-style events for incident and operational replay.
- Keep telemetry and incident summaries aligned to the same deterministic run context.
- Design modules so failures are diagnosable from artifacts without external tooling.

## 6. Public-Safe Output Boundary

- Keep internal detailed outputs in `reports/`.
- Publish sanitized outputs through `artifacts/showcase/public/`.
- Do not mix internal and public-safe artifacts in the same path.

## 7. Safe Extension Pattern

- Extend by adding one bounded module at a time.
- Update docs, tests, and artifact contracts in the same change.
- Avoid introducing non-deterministic dependencies into baseline paths.

## 8. Learning Progression

Recommended progression:

1. Build fundamentals in `quant-systems-lab`.
2. Integrate controls and operations in this repository.
3. Move to asynchronous multi-agent experimentation in `The Slippage Engine`.

This sequence keeps complexity layered and grounded in evidence.
