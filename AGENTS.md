# GameLovers Statechart — Agent Guide

This guide adds package-specific rules to the host repository guide. Consumer usage belongs in `README.md`.

## Scope

- Package: `com.gamelovers.statechart`; minimum Unity version and dependencies are authoritative in `package.json`.
- One runtime assembly implements hierarchical statecharts with nested and parallel regions, events, choices, callback waits, and Task/UniTask waits.
- There is no Editor assembly or package sample. This package is render-pipeline-neutral.
- Public contracts live at `Runtime/`; implementations under `Runtime/Internal/` remain internal.

## Statechart invariants

- A `Statechart` is fully defined by its constructor setup closure. Runtime mutation of the state graph is unsupported, and every region requires exactly one initial state.
- `Trigger` is a no-op until `Run()` and while paused. `Pause()` preserves the active position. `Reset()` returns to the initial state without changing the run/pause state.
- Statechart events compare by generated instance id, not by name. Reuse one event instance for one logical event.
- Every transition must name a target. A choice with no satisfied condition remains in the choice state.
- `Nest` completes one sequential child region. `Split` completes only after all parallel child regions finish.
- `NestedStateData` separately controls whether leaving a nested region executes the parent nest state's exit callback and the child final state's enter callback. The plain setup-closure overload enables both; preserve the distinction when changing leave/completion behavior.
- `Leave` exits exactly one region layer and bypasses the current nest/split completion transition.
- `IWaitState` may process events and uses `IWaitActivity` completion. `ITaskWaitState` cannot process events while awaiting its Task/UniTask; ancestor exit waits for it and queues intervening events.
- When an active callback wait is abandoned by an ancestor region exit, complete its outstanding inner activities rather than leaving them dangling. Task waits instead delay the exit until the task completes.
- Validation runs only under `UNITY_EDITOR || DEBUG`. Do not treat release-build absence of validation as a supported malformed graph.
- `CurrentState` is Editor-only. Runtime game logic must not depend on it.

## Change placement

- A new state type requires a public capability-composed interface, an internal implementation, a factory method on `IStateFactory`, and the matching `StateFactory` implementation.
- Keep the public surface expressed through narrow capability interfaces in `Runtime/IState.cs`; consumers should not depend on internal concrete states.
- Preserve the existing Task and UniTask overloads when changing async waiting.
- Before changing anything under `Tests/`, read `Tests/AGENTS.md`.

## Verification and documentation

- Test lifecycle, transition target validation, nested/split completion, leave depth, and waiting/event ordering when those areas change.
- Validation tests must run with Editor or DEBUG behavior enabled and must not imply equivalent release-build enforcement.
- Prefer the local UniTask package source under `Library/PackageCache/` for external API questions.
- Update `README.md` for public statechart semantics and this guide only for durable invariants or test conventions.
