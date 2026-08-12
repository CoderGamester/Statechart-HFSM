# GameLovers.Statechart - AI Agent Guide

> **Companion files**: `CLAUDE.md` wraps this file for Claude Code — edit `AGENTS.md`, not `CLAUDE.md`. `README.md` is the user-facing entry point.

## 1. Package Overview
- **Package**: `com.gamelovers.statechart`
- **Unity**: minimum 6000.0; compatibility reference streams 6000.0.x, 6000.3.x, and 6000.5.x. Reference editors: 6000.0.81f1, 6000.3.21f1, 6000.5.7f1 (primary). Do not call a stream validated without current matrix artifacts.
- **Dependencies** (see `package.json`)
  - `com.cysharp.unitask` (2.5.10): `ITaskWaitState.WaitingFor(Func<UniTask>)` overload

Hierarchical State Machine (HFSM / Statechart, per the [UML spec](http://www.omg.org/spec/UML) and [statecharts.github.io](https://statecharts.github.io/what-is-a-statechart.html)) — states can nest, split into parallel regions, and run async waits, all defined once in a constructor setup closure with no further runtime mutation. Smallest package in the family: no `Editor/` assembly, no `Samples~/`, single `Runtime/` assembly.

## 2. Runtime Architecture (high level)

### The chart itself
- **`Statechart`** (`Runtime/Statechart.cs`, implements `IStatechart : IStateMachineDebug`) is the entry point. Construct with `new Statechart(Action<IStateFactory> setup)` — the setup closure runs immediately and defines every state and transition; there is no API to add states after construction. Throws `MissingMemberException` if the setup never calls `factory.Initial(...)`.
  - `Run()` — starts/resumes execution from wherever the chart is anchored. No-op if already running.
  - `Trigger(IStatechartEvent trigger)` — processes an event with run-to-completion semantics. **No-op if the chart isn't running** (`Pause()`d or never `Run()` called) — a common "my event did nothing" cause.
  - `Pause()` — stops processing; `Run()` resumes from the same point.
  - `Reset()` — jumps back to the initial state. Does **not** implicitly pause or resume; if the chart was waiting on an event, it needs a fresh `Run()` to continue after reset.
  - `LogsEnabled` (from `IStateMachineDebug`) — per-chart debug logging toggle; `IState.LogsEnabled` is the same toggle per-state.
  - `CurrentState` (string name) is exposed **only under `#if UNITY_EDITOR`** — do not reference it from a runtime code path that also needs to compile for a player build.
  - In `#if UNITY_EDITOR || DEBUG`, every state's `Validate()` runs once at construction — this is where the "missing transition target" / "transition loop" exceptions below come from.
- **`IStateFactory`** (`Runtime/IStateFactory.cs`) is passed into the setup closure — one factory instance per region (top-level chart, and one more per `Nest`/`Split` sub-region). Each factory method takes a `name` string (debug-only identity) and returns the state's own narrow interface:

  | Factory method | Returns | Purpose |
  |---|---|---|
  | `Initial(name)` | `IInitialState` | Entry point of the region. Exactly one per region. |
  | `Final(name)` | `IFinalState` | Marks the region complete. |
  | `State(name)` | `ISimpleState` | Blocks until an `Event(...)` transition fires. |
  | `Transition(name)` | `ITransitionState` | Non-blocking; falls through to its target immediately. |
  | `Nest(name)` | `INestState` | Opens one new nested (sequential) region. |
  | `Choice(name)` | `IChoiceState` | Non-blocking; picks among `Transition().Condition(...)` branches. |
  | `Wait(name)` | `IWaitState` | Blocks on `IWaitActivity` completion (and/or an event). |
  | `TaskWait(name)` | `ITaskWaitState` | Blocks on a `Task`/`UniTask`; cannot process events while waiting. |
  | `Split(name)` | `ISplitState` | Opens two-or-more new nested **parallel** regions. |
  | `Leave(name)` | `ILeaveState` | Like `Final`, but its transition targets a state in an **ancestor** region, jumping out of the current `Nest`/`Split` by exactly one layer. |

### State capability interfaces (`Runtime/IState.cs`)
Every concrete state interface above is composed from these narrower capability interfaces — check which ones a state implements to know what it can do:
- `IStateEnter.OnEnter(Action)` / `IStateExit.OnExit(Action)` — lifecycle callbacks.
- `IStateTransition.Transition()` → `ITransition` — unconditional transition (used by `Initial`/`Transition`/`Leave` states).
- `IStateEvent.Event(IStatechartEvent)` → `ITransition` — event-triggered transition (used by `State`/`Nest`/`Wait`/`Split` states).
- `ITransition.OnTransition(Action)` (chainable) + `.Target(IState)` (terminal — every transition must call this or `Statechart`'s validation throws).
- `ITransitionCondition : ITransition` adds `.Condition(Func<bool>)` — used exclusively by `IChoiceState.Transition()`; a choice with a failing condition does not transition (evaluate carefully — a choice state with no satisfied condition stalls the chart there).

### Nesting and parallelism
- **`INestState.Nest(Action<IStateFactory> | NestedStateData)`** opens a new sequential sub-region; its returned `ITransition` fires once that sub-region reaches its `Final`. The `NestedStateData` overload (`Setup` + `ExecuteExit` + `ExecuteFinal` bools) controls whether the *parent* nest state's own `OnExit` and the *sub-region's* `IFinalState.OnEnter` actually run when leaving via a `Leave` state from inside vs. via normal completion — the plain `Action<IStateFactory>` overload defaults both to `true`.
- **`ISplitState.Split(params Action<IStateFactory>[] | NestedStateData[])`** opens N parallel sub-regions simultaneously; its `ITransition` fires only once **all** sub-regions reach their own `Final`.
- **`ILeaveState`** exits a `Nest`/`Split` region early, targeting a state in the parent region directly — bypasses the nest/split's own completion transition. Can only jump one region layer (an inner `Leave` inside a doubly-nested region still only reaches its immediate parent, not the top level).

### Waiting states
- **`IWaitState.WaitingFor(Action<IWaitActivity>)`** — the action receives an `IWaitActivity` (`Runtime/IWaitActivity.cs`); call `.Complete()` when the awaited work finishes, or `.Split()` first to fan out into multiple sub-activities whose *own* `.Complete()` calls all must return true before the parent completes. A `Wait` state also still processes `Event(...)` transitions (checked after `WaitingFor` since concurrency needs `WaitingFor` resolved first) — the doc comment on `IWaitState` explicitly notes this ordering. If a wait state is the active state when an ancestor `Nest`/`Split` exits, it force-completes itself and all inner activities rather than leaving them dangling.
- **`ITaskWaitState.WaitingFor(Func<Task> | Func<UniTask>)`** — blocks on true async work. Unlike `IWaitState`, **cannot process `Event(...)` transitions while waiting** (no `IStateEvent` in its interface composition) — if an ancestor region exits mid-wait, the exit itself is paused and any events that arrive during the wait are queued rather than dropped, to avoid a concurrency bottleneck.

### Events
- **`IStatechartEvent`** (`Runtime/StatechartEvent.cs`) — equality is by an auto-incrementing `uint Id` assigned at construction, **not** by `Name`. Two `new StatechartEvent("Jump")` instances are never equal to each other; keep one instance per logical event and reuse it across every `.Event(theSameInstance)` call site that should respond to it.

## 3. Key Directories / Files
- **Public interfaces** (root of `Runtime/`): `IState.cs` (all state-capability + concrete-state interfaces + `NestedStateData`), `ITransition.cs`, `IStateFactory.cs`, `IWaitActivity.cs`, `Statechart.cs` (+ `IStatechart`/`IStateMachineDebug`), `StatechartEvent.cs` (+ `IStatechartEvent`).
- **`Runtime/Internal/*`** — concrete state implementations (`InitialState`, `FinalState`, `SimpleState`, `TransitionState`, `NestState`, `SplitState`, `ChoiceState`, `WaitState`, `TaskWaitState`, `LeaveState`), `StateFactory`, `Transition`, `InnerStateData`, `StatechartUtils`. All `internal` — not part of the public surface; consumers only ever see the interfaces above.
- **Tests**: `Tests/Editor/*` (one asmdef, `GameLovers.Statechart.Editor.Tests`) — `StatechartTest.cs` (core lifecycle + validation-exception coverage), `StatechartStateTest.cs`, `StatechartTransitionTest.cs`, `StatechartChoiceTest.cs`, `StatechartNestTest.cs`, `StatechartSplitTest.cs`, `StatechartWaitTest.cs`, `StatechartTaskWaitTest.cs`, `StatechartLeaveTest.cs`, `StatechartNestSplit_IntegrationTest.cs`, `IMockCaler.cs` (mocked-callback interface used across the suite via NSubstitute). Before reading, editing, or creating any file in `Tests/`, you **MUST** read [`Tests/AGENTS.md`](Tests/AGENTS.md) first.
- **No `Editor/` assembly, no `Samples~/`, no `docs/`** — this package's entire surface is the interfaces above; there is no editor tooling and nothing to import as a sample.

## 4. Important Behaviors / Gotchas
- **Setup-time validation, not always-on**: the exceptions below (`MissingMemberException`, `InvalidOperationException`) only fire from the `#if UNITY_EDITOR || DEBUG` validation pass in the `Statechart` constructor — a malformed chart in a release build without `DEBUG` defined will not be caught the same way. Always validate in-editor / in tests before shipping a chart's setup.
- **`Trigger` is a no-op unless running**: calling `Trigger(...)` before the first `Run()`, or after `Pause()`, silently does nothing — it does not queue the event for later.
- **Event identity is per-instance, not per-name**: see `IStatechartEvent` above — a fresh `new StatechartEvent("X")` never equals a previously created `"X"` event. Store event instances as fields/constants, not locals recreated per call.
- **`ITaskWaitState` cannot receive events while waiting**; `IWaitState` can, but only after its `WaitingFor` activities resolve first in a concurrent scenario. Picking the wrong one of these two for a state that needs to react to an event mid-wait is a common design mistake.
- **`ILeaveState` only jumps one region layer** — from inside a doubly-nested region, a `Leave` still only reaches the immediate parent, not further up.
- **`CurrentState` is Editor/DEBUG-only surface** on `Statechart` — don't wire game logic to it.

## 5. Coding Standards (Unity 6 / C# 9.0)
- **C#**: C# 9.0 syntax; explicit namespaces (`GameLovers.StatechartMachine` for Runtime, `GameLoversEditor.StatechartMachine.Tests` for Tests); no global usings.
- **Assemblies**: `Runtime/GameLovers.Statechart.asmdef` has no Editor/UnityEditor reference — this package has no Editor assembly at all. Keep everything under `Runtime/Internal/` truly `internal`; the public surface is exactly the interfaces in section 2/3.
- **Async**: `Cysharp.Threading.Tasks` (UniTask) only appears in `ITaskWaitState.WaitingFor(Func<UniTask>)` — the plain-`Task` overload exists for consumers who don't want the UniTask dependency in their own state-setup code (the package dependency itself is unconditional either way).

## 6. External Package Sources (for API lookups)
- UniTask: `Library/PackageCache/com.cysharp.unitask/`

## 7. Common change workflows
- **Add a new state type**: define its public capability-composed interface in `Runtime/IState.cs`, add the corresponding `internal` implementation under `Runtime/Internal/`, and add the factory method to `IStateFactory` + `Runtime/Internal/StateFactory.cs`. Add test coverage under `Tests/Editor/` following the existing `Statechart<Type>Test.cs` naming.
- **Change validation behavior**: the per-state `Validate()` calls happen in `Statechart`'s constructor under `#if UNITY_EDITOR || DEBUG` — keep new validation failures as exceptions thrown from state `Validate()` implementations under `Runtime/Internal/`, matching the existing `MissingMemberException`/`InvalidOperationException` pattern.

## 8. Update Policy
Update this file when:
- Public API changes (`IStatechart`, any state-capability interface in `IState.cs`, `IStateFactory`, `ITransition`, `IWaitActivity`, `IStatechartEvent`)
- Validation/exception behavior changes in the `Statechart` constructor's setup pass
- Nesting/splitting/leaving semantics change (region completion rules, `NestedStateData` flag behavior)
- Dependencies in `package.json` change (cross-check this file and `README.md` for stale references)
