# RDMP CI Stabilization

## What This Is

Fix CI test stability after migrating to Microsoft Testing Platform (MTP). The Linux CI run currently times out at 45 minutes, blocking this PR and 4-5 dependabot updates behind it. This milestone gets CI green and PRs flowing again.

## Core Value

CI passes reliably so PRs can merge.

## Requirements

### Validated

- ✓ MTP test runner configured — existing (`global.json`, `Directory.Build.props`)
- ✓ 60-second per-test timeout — existing (`AssemblyInfo.cs`)
- ✓ Parallel test execution — existing
- ✓ Multi-database test support (SQL Server, MySQL, PostgreSQL) — existing CI infrastructure

### Active

- [ ] CI completes within 45-minute timeout
- [ ] Verbose test output identifies hanging tests (`--output Detailed`)
- [ ] Root cause of test hang identified and fixed
- [ ] PR `fix/mtp-aws-credentials` merged to main
- [ ] Dependabot PRs rebased and merged

### Out of Scope

- Local test execution setup — tests require multiple DB engines, CI-only for now
- New test coverage — focus is stability, not expansion
- VSTest fallback — committed to MTP, not reverting

## Context

**Current state:**
- Branch `fix/mtp-aws-credentials` has CI fixes in progress
- Just fixed: `--logger` → `--output Detailed` (MTP-compatible verbose output)
- Waiting for: CI run to show where tests hang

**Technical environment:**
- .NET 9.0 / 10.0 multi-targeted
- MTP configured via `EnableNUnitRunner=true`, `TestingPlatformDotnetTestSupport=true`
- CI runs on GitHub Actions (Linux for tests, Windows for UI)
- Tests use MinIO for S3-compatible storage in CI

**Known from codebase analysis:**
- Tests share database connection pool (potential exhaustion)
- Some tests use `Thread.Sleep()` which can delay completion
- MEF type discovery has dual paths (compiled registry vs reflection fallback)

## Constraints

- **CI-only debugging**: Tests require multiple DB engines not available locally
- **45-minute iteration cycles**: Each CI run takes up to timeout to fail
- **MTP commitment**: Not reverting to VSTest, must fix forward

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Use `--output Detailed` for verbose output | MTP doesn't support VSTest `--logger` option | — Pending (waiting for CI) |
| Keep MTP, don't revert to VSTest | MTP is the future, VSTest is legacy | — Pending |

---
*Last updated: 2026-01-26 after initialization*
