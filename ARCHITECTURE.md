# Architecture

This document defines how `declar` moves to a hexagonal architecture (ports and adapters).

## Goals

- Keep core logic independent from CLI, shell commands, and file system details.
- Make each declaration use-case testable without running real system commands.
- Add new vendors and file resources as adapters, not core rewrites.
- Preserve current CLI UX: `declar <command> <declaration> <inputs> [flags]`.

## Current State (Short)

Current code works, but core and infrastructure are mixed in places:

- `Declar.Cli` owns command discovery, execution flow, and many concrete declaration implementations.
- Declarations call concrete runtime concerns directly (`File`, process execution via `CommandRunner`).
- `Declar.Core` has useful contracts (`ICommand`, `IDeclaration`, `ICommandShell`, `ITerminalReporter`) but no strong application boundary yet.

## Target Hexagon

Hexagonal model for Declar:

- Domain: pure rules and state intent.
- Application: use-cases that orchestrate domain + ports.
- Ports: interfaces for outbound concerns (shell, files, host info, reporting).
- Adapters: CLI, process runner, real file access, terminal output, vendor-specific command mapping.

Dependency rule:

- Adapters -> Application -> Domain
- Domain depends on nothing outside itself.
- Application depends only on Domain + Port interfaces.

## Proposed Solution Layout

```
src/
├── Declar.Domain/              # Entities, value objects, policies
├── Declar.Application/         # Use-cases, input/output models, ports
├── Declar.Infrastructure/      # Outbound adapters (shell, files, host, vendor executors)
├── Declar.Cli/                 # Inbound adapter (args, composition root, terminal UX)
├── Declar.Domain.Test/
├── Declar.Application.Test/
└── Declar.Cli.Test/
```

Notes:

- Existing `Declar.Core` can be split gradually into `Declar.Domain` and `Declar.Application`.
- Keep `Declar.Cli` as the composition root while migrating.

## Core Concepts

### Domain

Domain models should describe intent, not shell syntax:

- `ResourceKind` (vendor package, hosts entry, later systemd unit, config key)
- `DesiredState` (installed, removed, commented, uncommented)
- `Declaration` (resource + desired state + one or more targets)
- `Outcome` (ok, changed, error + reason)

Domain services/policies:

- idempotency rules
- normalization rules (for example hosts line normalization)
- conflict and validation rules

### Application (Use-Cases)

Main use-case for CLI execution:

- `ApplyDeclarationUseCase`
    - validate command/declaration/inputs
    - resolve matching resource handler
    - execute idempotent apply flow
    - emit structured progress and result events

Secondary use-cases (later):

- `ProbeDeclarationUseCase`
- `ExplainPlanUseCase` (good for `--test` / dry-run output)

### Ports (Outbound)

Define ports in `Declar.Application`:

- `IProcessPort` - run/probe external commands
- `IFilePort` - read/write files and ensure atomic updates where needed
- `IHostPort` - OS/distro/capability queries
- `IReporterPort` - progress/result reporting events
- `IClockPort` - cache freshness and deterministic tests

Keep ports small and purpose-specific.

### Adapters

Inbound adapter:

- CLI parser maps argv -> `ApplyDeclarationRequest`

Outbound adapters:

- process adapter wraps `ProcessStartInfo`
- filesystem adapter wraps `File` I/O
- terminal adapter prints `StatementStatus`
- vendor adapters map a domain action to concrete package manager commands

## Command and Declaration Mapping

Today: command classes (`AptCommand`, `HostsCommand`, etc.) bundle behavior and transport details.

Target:

- command name + declaration name map to a use-case handler key
- handler uses ports and domain policy
- vendor-specific shell command text lives in infrastructure adapter, not in use-case logic

Example mapping table concept:

- `apt installed <pkg>` -> `PackageStateHandler(vendor: apt, desired: installed)`
- `apt removed <pkg>` -> `PackageStateHandler(vendor: apt, desired: removed)`
- `hosts commented <line>` -> `HostsEntryStateHandler(desired: commented)`

## Migration Plan (Incremental)

### Phase 1 - Stabilize Contracts

- Keep current behavior.
- Introduce `Declar.Application` with request/response models.
- Move `CommandContext`-style runtime dependencies behind new application ports.
- Add adapter shims in CLI so old commands can still run.

Exit criteria:

- CLI calls application entry point for at least one command path.

### Phase 2 - Extract First Vertical Slice

- Migrate `hosts` declarations to application + domain + infra adapters.
- Move hosts normalization logic into domain policy.
- Keep CLI syntax and output unchanged.

Exit criteria:

- `hosts` path has no direct `File` access in CLI project.

### Phase 3 - Vendor Package Slice

- Migrate one vendor first (`apt`) using a generic package use-case.
- Introduce vendor command adapter strategy for probe/install/remove.
- Reuse same use-case for `dnf`, `pacman`, `winget`, etc.

Exit criteria:

- vendor command classes become thin mapping/adapters or are removed.

### Phase 4 - Full Composition + Cleanup

- Route all commands through application use-cases.
- Move remaining core contracts from `Declar.Core` to new projects.
- Remove obsolete command/declaration classes and temporary shims.

Exit criteria:

- clean dependency flow: `Cli` -> `Application` -> `Domain`; `Infrastructure` plugged in via interfaces.

## Testing Strategy

- Domain tests: pure rule tests, no mocks.
- Application tests: use fake ports for process/file/host/reporter.
- Adapter tests: focused integration tests for process and filesystem behavior.
- CLI tests: argument and output behavior only.

## Practical First Refactors

Suggested first steps in code:

1. Add `ApplyDeclarationRequest` and `ApplyDeclarationResult` in application layer.
2. Create `IDeclarationHandler` abstraction in application.
3. Implement `HostsEntryHandler` using `IFilePort` + domain normalization policy.
4. Make `Program.cs` call one application entry point instead of direct command class execution.

These steps give a real hexagonal slice with low risk.
