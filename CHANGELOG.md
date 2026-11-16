# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-16

**New**:
- **Statechart Debugger**: *Tools > GameLovers > Statechart Debugger* shows any running chart live, with Run, Pause and Reset controls and two views that can each be turned on or off: a live chart of nested boxes with parallel regions as lanes, and a state tree with the latest 256 trace steps. The **Copy** menu exports a Mermaid diagram, the active state paths or the trace.
- **Chart view**: `IStatechart.RootRegion` exposes the chart's structure through `IRegionNode`, `IStateNode` and `ITransitionEdge`, and `GetActiveStates` and the allocation-free `IsActive` report which states are active. The view is the live chart itself, so it costs nothing to build.
- **Trace**: `IStatechart.Traced` publishes every event, exit, transition, enter, fault and lifecycle step in execution order as a `StatechartTrace`; nothing is allocated while no one listens.
- **Lifecycle**: a `Statechart(string name, Action<IStateFactory> setup)` constructor, `Name` and `IsRunning`, and `IDisposable` support. `Dispose()` stops the chart for good, cancels token-aware tasks and removes it from the debugger; `Run()` after it throws `ObjectDisposedException`.
- **Cancellable task waits**: `ITaskWaitState.WaitingFor` overloads receive a `CancellationToken` that `Reset()` and `Dispose()` cancel.
- **Game Flow sample**: one statechart drives a small game loop that uses every state kind, with on-screen controls, meant to be watched in the Statechart Debugger.

**Changed**:
- The minimum Unity version is now 6000.0 (was 2022.3); 6000.0.x, 6000.3.x and 6000.6.x are the supported streams.
- Requires UniTask 2.5.11 (was 2.5.10), the first version whose Editor code compiles on Unity 6000.6, and the built-in UIElements module used by the debugger and the sample.
- Events triggered while another event is being processed, for example from an enter callback, are now queued and processed right after it (run to completion) instead of running against the previous state.
- `Reset()` now drops queued events and abandons pending waits and tasks, so their late completion no longer moves the chart.
- `LogsEnabled` now writes one trace line per step, such as `[Statechart 'Player'] Idle -> Jumping on 'Jump'`.
- Null arguments now throw `ArgumentNullException`, and a setup without an initial state throws `InvalidOperationException` instead of `MissingMemberException`.

**Removed**:
- The Editor-only `Statechart.CurrentState`.

**Migration**:
- Replace `CurrentState` with `GetActiveStates`, `IsActive` or the Statechart Debugger.
- Custom `IStatechart` implementations must add the new members: `Name`, `IsRunning`, `RootRegion`, `Traced`, `GetActiveStates`, `IsActive` and `Dispose`.
- Catch `InvalidOperationException` instead of `MissingMemberException` for a setup without an initial state.

**Fixed**:
- A `TaskWait` state whose task throws no longer blocks the chart and its parent splits forever; the fault is logged and the state completes.
- `TaskWait` states now resume the chart on the main thread, and a `Task` completing on the main thread resumes it in the same frame.
- Pausing or resetting the chart from a callback now stops a chain of automatic transitions.
- An action-only event on a nest or split state no longer force-completes the waits inside it.
- Creation stack traces in error logs now start at the code that declared the failing state, and enter-callback errors no longer report themselves as exit errors.

**Docs**:
- Rewrote the README with state kinds, runtime semantics, the observe API, the debugger and the sample.

## [0.9.5] - 2026-08-04

**Changed**:
- Replaced the Unity Package Starter Kit README with accurate Statechart usage and API guidance based on the package's actual runtime surface.
- Improved public API documentation and automated coverage of existing state creation, transition, nested/split, waiting, and validation behavior; this release does not change runtime behavior.

## [0.9.4] - 2026-06-26

**Fixed**
- Added guard in *Statechart.MoveNext* to halt chained state transitions when the State Chart is paused

**Changed**
- Removed *openai.yml* GitHub Actions workflow

## [0.9.3] - 2024-11-13

**New**
- Added new *ITaskWaitState.WaitingFor(Func<UniTask>)* method to allow *TaskWaitStates* to hold on *UniTask* type calls

**Fixed**
- Added *UniTask* to the package to allow the *Statechart* run on WebGL

## [0.9.2] - 2024-10-25

**Fixed**
- Changed the try/catch exceptions to Debug.LogException with a Debug.LogError, to properly show the issues in the Unity console

## [0.9.1] - 2023-09-04

**Fixed**
- Added missing documentation to describe the purpose of the *InnerStateData* and *StateFactoryData* classes.

## [0.9.0] - 2023-09-02

**New**
- Introduced unique identifiers for *IWaitActivity* instances to differentiate between different activities
- Enhanced the *TaskWaitState* class with a new event queuing mechanism to handle events after task completion
- Improved the *SplitState* class to allow pausing/resuming of inner state transitions

**Changed**
- Updated exception handling across various classes, replacing *MissingMemberException* with *InvalidOperationException* for more appropriate error representation

## [0.8.0] - 2023-08-03

**Changed**
- Change the main class name from *Statemachine* to *Statechart* Change namespace to avoid conflicts to StatechartMachine
- Changed both *ISplitState* and *INestedState* to receive a single parameterized *NestedStateData* to make calls less verbose and ready for future scalability

## [0.7.0] - 2021-05-04

**New**
- Improved State documentations
- Improved State debug logs

**Changed**
- Removed redundant *SplitFinal* & *SplitExitFinal* methods from *ISplitState*. Now the *Split* method incorporates all possible cases to simplify implementation

**Fixed**
- Fixed reference checks for *WaitState*
- Fixed *StatechartTaskWaitTest* to work also when multiple tests run in parallel
- Fixed *TaskWaitState* racing condition bug on chained states
- Fixed bug on *WaitState* that when finished instantaneously would execute state transitions multiple times

## [0.6.0] - 2020-09-29

**Changed**
- Renamed *Statechart* to *StateMachine* to avoid namespace conflicts

**Fixed**
- Fixed *ITaskWaitState* bad tests

## [0.5.2] - 2020-09-27

**New**
- Added *IStatechartEvent* data to the logs
- Added logs to all possible trigger cases
- Added logs to the *ITaskWaitState* and *IWaitState* waiting call method
- Added exception catch to the *ITaskWaitState* and *IWaitState* waiting call method
- Added the possibility to enable logs individually to each *IState*

## [0.5.1] - 2020-09-24

**Fixed**
- Fixed *ITaskWaitState* execution crash

## [0.5.0] - 2020-09-24

**New**
- Added new *ITransitionState* that acts as a non-blocker state between 2 different states

**Changed**
- Added *IStateEvent* to *ITaskWaitState* to allow event evocation 

**Fixed**
- Fixed potential issue when *ITaskWaitState* & *WaitState* would execute it's flow multiple times when a *INestState* or *ISplitState* would trigger an outside exit event	

## [0.4.0] - 2020-09-22

**New**
- Added the possibility to not execute *IStateExit.OnExit* on the current active state when leaving *IStateNest* or *IStateSplit*.
- Added the new *ITaskWaitState* to have a waiter for Task async methods. This state cannot have event triggers, for that use *IWaitState*

**Changed**
- Separated *IStateSplit.Split* into new method *IStateSplit.SplitFinal*

## [0.3.0] - 2020-09-06

**New**
- Added the possibility to trigger events without a target state. Only *InitialState*, *ChoiceState* & *LeaveState* don't allow it due to the nature of their behaviour.

## [0.2.0] - 2020-08-27

**New**
- Added the possibility to always execute the *FinalState* of a *NestState* and * *SplitState*

**Changed**
- Splitted the Unit tests in different files to help organization and readability. Now each state has it's own file with only it's set of tests

**Fixed**
- Now *NestState* and * *SplitState* properly execute their inner *OnExit* calls when they are setup in a chain of nested states

## [0.1.3] - 2020-01-06

**Fixed**
- Removed package dependency

## [0.1.2] - 2020-01-06

**New**
- Added NSubstitute dependency for the Unit Tests

**Fixed**
- Removed Preview label out of the package version

## [0.1.0] - 2020-01-05

- Initial submission for package distribution
