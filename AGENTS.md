# GameLovers Statechart — Agent Guide

This guide adds package-specific rules to the host repository guide. Consumer usage belongs in `README.md`.

## Scope

- Package: `com.gamelovers.statechart`; minimum Unity version and dependencies are authoritative in `package.json`. Supported streams are 6000.0, 6000.3 and 6000.6.
- `GameLovers.Statechart` (runtime) implements hierarchical statecharts with nested and parallel regions, events, choices, callback waits, Task/UniTask waits, and the read-only topology, active-state and trace API.
- `GameLovers.Statechart.Editor` holds the Statechart Debugger.
- `Samples~/GameFlow` is the single package sample. This package is render-pipeline-neutral.
- Public contracts live at `Runtime/`; implementations under `Runtime/Internal/` remain internal. `Runtime/AssemblyInfo.cs` grants internals to the Editor and Editor test assemblies only.

## Statechart invariants

- A `Statechart` is fully defined by its constructor setup closure. Runtime mutation of the state graph is unsupported, and every region requires exactly one initial state. The read-only view (`IRegionNode`, `IStateNode`, `ITransitionEdge`) is implemented by the live `StateFactory`, states and `Transition` objects; do not reintroduce a parallel node graph. Each state records its edges through `StateInternal.CreateTransition`.
- `Trigger` is a no-op until `Run()` and while paused. `Pause()` preserves the active position and stops a chain of automatic transitions mid-way when called from a callback.
- Events are processed run to completion: an event triggered while `MoveNext` is processing is queued and drained after the current step. Do not reintroduce re-entrant processing.
- `Reset()` returns to the initial state without exit callbacks or a run/pause change. It drops queued events and cancels the chart token, then starts a new one. Waits and tasks capture the token when they start and ignore their completion once it is cancelled; it is the only stale-completion guard. `Dispose()` cancels the same way and stops the chart for good.
- Statechart events compare by generated instance id, not by name. Reuse one event instance for one logical event.
- Event transitions on simple, wait, nest and split states may omit a target to run an action only; such an event leaves inner waits running. Initial, transition, choice, leave and completion transitions need a target. A choice with no satisfied condition remains in the choice state.
- `Nest` completes one sequential child region. `Split` completes only after all parallel child regions finish.
- `NestedStateData` separately controls whether leaving a nested region executes the parent nest state's exit callback and the child final state's enter callback. The plain setup-closure overload enables both; preserve the distinction when changing leave/completion behavior.
- `Leave` exits exactly one region layer and bypasses the current nest/split completion transition.
- `IWaitState` may process events and uses `IWaitActivity` completion. `ITaskWaitState` cannot process events while awaiting its task; ancestor exit waits for it and queues intervening events.
- A faulted task completes its `TaskWait` state after the fault is reported. Task completions resume on the main thread, and a `Task` completing on the main thread resumes inline; keep `AwaitTask` rather than `AsUniTask`, which posts to a later frame.
- When an active callback wait is abandoned by an ancestor region exit, complete its outstanding inner activities rather than leaving them dangling. Task waits instead delay the exit until the task completes.
- Every observable step goes through `StateInternal.Trace` / `Statechart.Emit`. `LogsEnabled` formats those traces; do not add separate `Debug.Log` paths. With no `Traced` subscriber and logs off, nothing is formatted.
- The runtime's only debugger hook is the Editor-only `Statechart.Created` event. `StatechartRegistry` and `StatechartExport` live in the Editor assembly; the registry holds charts weakly, records history as ids and text, and drops a chart on its `Disposed` trace.
- Validation runs only under `UNITY_EDITOR || DEBUG`. Do not treat release-build absence of validation as a supported malformed graph.

## Change placement

- A new state type requires a public capability-composed interface, an internal implementation with a `StateKind` value that creates its transitions with `CreateTransition`, a factory method on `IStateFactory`, and the matching `StateFactory` implementation. Update `Editor/StatechartExport` and the debugger views if the kind needs special drawing.
- Keep the public surface expressed through narrow capability interfaces in `Runtime/IState.cs`; consumers should not depend on internal concrete states.
- Preserve the existing Task and UniTask overloads, with and without a `CancellationToken`, when changing async waiting.
- Tools read the chart only through `RootRegion`, `GetActiveStates`, `IsActive` and `Traced`, and discover it through `Statechart.Created`. Do not add view-specific hooks to the runtime.
- Sample code stays in `Samples~/GameFlow` with its own asmdef; package assemblies never reference it. After changing `Samples~`, refresh and validate the imported copy as the host guide requires.
- Before changing anything under `Tests/`, read `Tests/AGENTS.md`.

## Statechart Debugger

- `StatechartDebuggerWindow` is a UI Toolkit `EditorWindow` under `Tools/GameLovers/Statechart Debugger`. Its toolbar acts on the selected live chart (Run, Pause, Reset); it never builds a throwaway chart.
- `LiveChartView` and `TreeTraceView` read the same three sources: `IStatechart.RootRegion`, `GetActiveStates`, and `StatechartRegistry.Entry.History`. A view must not need its own runtime data. Toolbar toggles named after each view turn it on or off.
- `StatechartRegistry` subscribes to `Statechart.Created`. History records keep state ids and paths, never `IStateNode` references: a node is a live state, so holding one keeps its chart alive.
- The window refreshes on a 100 ms schedule and repaints views only when `StatechartRegistry.Version` changes. Keep per-refresh work proportional to the chart size and free of allocations beyond the active list.
- Styles live in `StatechartDebugger.uss`, loaded by package path. Use colors that read in both Editor themes.
- A Graph Toolkit view was tried and removed: on 6000.6 its flat graph hid nest and split hierarchy, and the API has no read-only or container node. Do not reintroduce it without those capabilities.

## Verification and documentation

- Test lifecycle, run-to-completion ordering, transition target validation, nested/split completion, leave depth, waiting/event ordering, and trace order when those areas change.
- Validation tests must run with Editor or DEBUG behavior enabled and must not imply equivalent release-build enforcement.
- Debugger and sample changes need a fresh Play-mode capture with the sample running and the affected view visible; green tests do not prove the views.
- Prefer the local UniTask package source under `Library/PackageCache/` for external API questions.
- Update `README.md` for public statechart semantics and this guide only for durable invariants or test conventions.
