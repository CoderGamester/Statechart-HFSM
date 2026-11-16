# GameLovers Statechart Editor — Agent Guide

This guide adds Editor-only rules to the package and host guides.

## Statechart Debugger

- `StatechartDebuggerWindow` is a UI Toolkit `EditorWindow` under `Tools/GameLovers/Statechart Debugger`. Its toolbar acts on the selected live chart (Run, Pause, Reset); it never builds a throwaway chart.
- `LiveChartView` and `TreeTraceView` read the same three sources: `IStatechart.RootRegion`, `GetActiveStates`, and `StatechartRegistry.Entry.History`. A view must not need its own runtime data. Toolbar toggles named after each view turn it on or off.
- `StatechartRegistry` subscribes to `Statechart.Created`. History records keep state ids and paths, never `IStateNode` references: a node is a live state, so holding one keeps its chart alive.
- The window refreshes on a 100 ms schedule and repaints views only when `StatechartRegistry.Version` changes. Keep per-refresh work proportional to the chart size and free of allocations beyond the active list.
- Styles live in `StatechartDebugger.uss`, loaded by package path. Use colors that read in both Editor themes.
- A Graph Toolkit view was tried and removed: on 6000.6 its flat graph hid nest and split hierarchy, and the API has no read-only or container node. Do not reintroduce it without those capabilities.
