#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${ROOT_DIR}"

FIXED_TS="${1:-2026-02-22T12:00:00Z}"

echo "==> Running tests"
dotnet test

echo
echo "==> Building reports + public showcase pack (${FIXED_TS})"
dotnet run --project src/FundLaunch.Platform.Cli -- reports showcase "--fixed-ts=${FIXED_TS}"

echo
echo "Showcase artifacts generated:"
echo "- Internal runtime reports: ${ROOT_DIR}/reports"
echo "- Public-safe showcase pack: ${ROOT_DIR}/artifacts/showcase/public"
