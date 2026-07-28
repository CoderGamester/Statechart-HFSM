# GameLovers Statechart

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)

A Hierarchical Finite State Machine (Statechart / HFSM) for Unity — states can nest into sub-regions, split into parallel regions, and block on async waits, all defined once in a single setup closure with no runtime mutation of the chart's shape.

## Why Use This Package?

Plain FSMs get unwieldy once a game state has sub-states of its own (a "Playing" state that is itself "Loading" → "Countdown" → "InProgress"), or needs two things happening at once (an animation playing while input is disabled). A Statechart — per the [UML spec](http://www.omg.org/spec/UML) and the broader [statecharts model](https://statecharts.github.io/what-is-a-statechart.html) — solves both by letting a state open its own nested region (`Nest`) or two parallel regions (`Split`), instead of flattening everything into one state graph.

### Key Features
- **10 state types** covering the common Statechart vocabulary: `Initial`, `Final`, `State` (event-blocking), `Transition` (pass-through), `Choice` (conditional branch), `Wait` (activity-blocking), `TaskWait` (async-blocking), `Nest` (sequential sub-region), `Split` (parallel sub-regions), `Leave` (early exit to a parent region).
- **Fluent setup** — one constructor closure defines the entire chart; no separate registration step.
- **Async-aware waiting** — `TaskWait` states block on a `Task` or `UniTask` directly.
- **Editor-time validation** — malformed setups (missing initial state, transition with no target, transition loops) throw immediately at construction in the Editor / Debug builds.

## System Requirements

- **[Unity](https://unity.com/download)** (v2022.3+) — the only package in the GameLovers family that doesn't require Unity 6
- **[UniTask](https://github.com/Cysharp/UniTask)** (v2.5.10+) — for the `ITaskWaitState.WaitingFor(Func<UniTask>)` overload

Dependencies are automatically resolved when installing via Unity Package Manager.

## Installation

### Via Unity Package Manager (Recommended)

1. Open Unity Package Manager (`Window` → `Package Manager`)
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/CoderGamester/Statechart-HFSM.git`

### Via manifest.json

```json
{
  "dependencies": {
    "com.gamelovers.statechart": "https://github.com/CoderGamester/Statechart-HFSM.git"
  }
}
```

## Key Components

| Type | Purpose |
|---|---|
| `Statechart` | The chart itself — `Run()` / `Pause()` / `Trigger(event)` / `Reset()` |
| `IStateFactory` | Passed into the setup closure; one factory method per state type (`Initial`, `Final`, `State`, `Transition`, `Choice`, `Wait`, `TaskWait`, `Nest`, `Split`, `Leave`) |
| `ITransition` / `ITransitionCondition` | `.OnTransition(action).Target(state)`; `Choice` transitions add `.Condition(() => bool)` |
| `IStatechartEvent` / `StatechartEvent` | Event identity is per-instance — keep one instance per logical event |
| `IWaitActivity` | Passed into a `Wait` state's `WaitingFor(...)`; call `.Complete()` to unblock, or `.Split()` to fan out |

## Quick Start

```csharp
using GameLovers.StatechartMachine;
using UnityEngine;

var jumpEvent = new StatechartEvent("Jump");

var statechart = new Statechart(factory =>
{
    var initial = factory.Initial("Initial");
    var idle = factory.State("Idle");
    var jumping = factory.State("Jumping");
    var final = factory.Final("Final");

    initial.Transition().Target(idle);

    idle.Event(jumpEvent).OnTransition(() => Debug.Log("Jumping!")).Target(jumping);
    idle.OnEnter(() => Debug.Log("Entered Idle"));

    jumping.OnEnter(() => Debug.Log("Entered Jumping"));
    jumping.Event(jumpEvent).Target(final); // second Jump ends the chart

    final.OnEnter(() => Debug.Log("Done"));
});

statechart.Run();
statechart.Trigger(jumpEvent); // Idle -> Jumping
statechart.Trigger(jumpEvent); // Jumping -> Final
```

Every state is created via the `factory` passed into the constructor closure — there is no separate registration call, and the chart's shape cannot be changed after construction. See [AGENTS.md](AGENTS.md) for nested regions (`Nest`), parallel regions (`Split`), async waits (`TaskWait`), and the full state-type reference.

## Related docs

| Document | Purpose |
|---|---|
| [AGENTS.md](AGENTS.md) | Contributor/agent guide — full state-type reference, architecture, gotchas |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

## Contributing

Contributions are welcome! See [AGENTS.md](AGENTS.md) for architecture details, coding standards, and common workflows.

## Support

- **Issues**: [Report bugs or request features](https://github.com/CoderGamester/Statechart-HFSM/issues)

## License

MIT — see [LICENSE.md](LICENSE.md).
