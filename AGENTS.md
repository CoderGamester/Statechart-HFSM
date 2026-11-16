# GameLovers Statechart — Agent Guide

This guide adds package-specific rules to the host repository guide. Consumer usage belongs in `README.md`.

## Scope

- Package: `com.gamelovers.statechart`; minimum Unity version and dependencies are authoritative in `package.json`. Supported streams are 6000.0, 6000.3 and 6000.6.
- `GameLovers.Statechart` (runtime) implements hierarchical statecharts with nested and parallel regions, events, choices, callback waits, Task/UniTask waits, and the read-only topology, active-state and trace API.
- `GameLovers.Statechart.Editor` holds the Statechart Debugger; `GameLovers.Statechart.Editor.GraphToolkit` holds view B and compiles only on 6000.6+.
- `Samples~/GameFlow` is the single package sample. This package is render-pipeline-neutral.
- Public contracts live at `Runtime/`; implementations under `Runtime/Internal/` remain internal. `Runtime/AssemblyInfo.cs` grants internals to the Editor, Graph Toolkit and Editor test assemblies only.
- Before reading, editing, or creating any file in `Editor/`, you **MUST** read [Editor/AGENTS.md](Editor/AGENTS.md) first.

## Statechart invariants

- A `Statechart` is fully defined by its constructor setup closure. Runtime mutation of the state graph is unsupported, and every region requires exactly one initial state. The public topology (`RootRegion`) is built once at the end of the constructor.
- `Trigger` is a no-op until `Run()` and while paused. `Pause()` preserves the active position and stops a chain of automatic transitions mid-way when called from a callback.
- Events are processed run to completion: an event triggered while `MoveNext` is processing is queued and drained after the current step. Do not reintroduce re-entrant processing.
- `Reset()` returns to the initial state without exit callbacks or a run/pause change. It increments the chart generation, drops queued events and cancels the chart token, so stale wait or task completions are ignored. `Dispose()` does the same, then stops the chart for good and unregisters it.
- Statechart events compare by generated instance id, not by name. Reuse one event instance for one logical event.
- Event transitions on simple, wait, nest and split states may omit a target to run an action only; such an event leaves inner waits running. Initial, transition, choice, leave and completion transitions need a target. A choice with no satisfied condition remains in the choice state.
- `Nest` completes one sequential child region. `Split` completes only after all parallel child regions finish.
- `NestedStateData` separately controls whether leaving a nested region executes the parent nest state's exit callback and the child final state's enter callback. The plain setup-closure overload enables both; preserve the distinction when changing leave/completion behavior.
- `Leave` exits exactly one region layer and bypasses the current nest/split completion transition.
- `IWaitState` may process events and uses `IWaitActivity` completion. `ITaskWaitState` cannot process events while awaiting its task; ancestor exit waits for it and queues intervening events.
- A faulted task completes its `TaskWait` state after the fault is reported. Task completions resume on the main thread, and a `Task` completing on the main thread resumes inline; keep `AwaitTask` rather than `AsUniTask`, which posts to a later frame.
- When an active callback wait is abandoned by an ancestor region exit, complete its outstanding inner activities rather than leaving them dangling. Task waits instead delay the exit until the task completes.
- Every observable step goes through `StateInternal.Trace` / `Statechart.Emit`. `LogsEnabled` formats those traces; do not add separate `Debug.Log` paths. With no `Traced` subscriber and logs off, nothing is formatted.
- `StatechartRegistry` is Editor-only, holds charts weakly and records trace history without a chart reference. Runtime game logic must not depend on it.
- Validation runs only under `UNITY_EDITOR || DEBUG`. Do not treat release-build absence of validation as a supported malformed graph.

## Change placement

- A new state type requires a public capability-composed interface, an internal implementation with a `StateKind` value and `CollectTransitions`, a factory method on `IStateFactory`, and the matching `StateFactory` implementation. Update `StatechartExport` and the debugger views if the kind needs special drawing.
- Keep the public surface expressed through narrow capability interfaces in `Runtime/IState.cs`; consumers should not depend on internal concrete states.
- Preserve the existing Task and UniTask overloads, with and without a `CancellationToken`, when changing async waiting.
- Tools read the chart only through `RootRegion`, `GetActiveStates` and `Traced`. Do not add view-specific hooks to the runtime.
- Sample code stays in `Samples~/GameFlow` with its own asmdef; package assemblies never reference it. After changing `Samples~`, refresh and validate the imported copy as the host guide requires.
- Before changing anything under `Tests/`, read `Tests/AGENTS.md`.

## Verification and documentation

- Test lifecycle, run-to-completion ordering, transition target validation, nested/split completion, leave depth, waiting/event ordering, and trace order when those areas change.
- Validation tests must run with Editor or DEBUG behavior enabled and must not imply equivalent release-build enforcement.
- Debugger and sample changes need a fresh Play-mode capture with the sample running and the affected view visible; green tests do not prove the views.
- Prefer the local UniTask package source under `Library/PackageCache/` for external API questions.
- Update `README.md` for public statechart semantics and this guide only for durable invariants or test conventions.
