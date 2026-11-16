# GameLovers Statechart Editor — Agent Guide

This guide adds Editor-only rules to the package and host guides.

## Statechart Debugger

- `StatechartDebuggerWindow` is a UI Toolkit `EditorWindow` under `Tools/GameLovers/Statechart Debugger`. Its toolbar acts on the selected live chart (Run, Pause, Reset); it never builds a throwaway chart.
- Views A (`LiveChartView`), B (`GraphToolkit/StatechartGraphView`) and C (`TreeTraceView`) read the same three sources: `IStatechart.RootRegion`, `GetActiveStates`, and `StatechartRegistry.Entry.History`. Keep them side-by-side comparable; a view must not need its own runtime data.
- The window refreshes on a 100 ms schedule and repaints views only when `StatechartRegistry.Version` changes. Keep per-refresh work proportional to the chart size and free of allocations beyond the active list.
- Styles live in `StatechartDebugger.uss`, loaded by package path. Use colors that read in both Editor themes.

## View B and Graph Toolkit

- `GameLovers.Statechart.Editor.GraphToolkit` is constrained by `UNITY_6000_6_OR_NEWER`, so it is absent on 6000.0 and 6000.3. The window only reaches it through `StatechartDebuggerWindow.GraphViewOpener`; never reference Graph Toolkit types from `GameLovers.Statechart.Editor`.
- Use only the public `Unity.GraphToolkit.Editor` API. Graph Toolkit is a built-in module from 6000.4; do not add `com.unity.graphtoolkit` to any manifest.
- The generated `.statechartdebug` asset under `Assets/StatechartDebugger/` is display-only and regenerated on every open. Live highlighting writes `Node.DefaultColor` and `Wire.WidthOverride` on the open window's graph inside one `UndoBeginRecordGraph`/`UndoEndRecordGraph` pair, only when `StatechartRegistry.Version` changes. On 6000.6.6f1 the window did not redraw `GraphVisualization` fills or unbracketed model changes; re-check that before switching channels.
