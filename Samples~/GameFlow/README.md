# Game Flow sample

One statechart drives a small game loop. Between them, its states use every state kind:

| State | Kind | What it shows |
| --- | --- | --- |
| Boot | Nest | A sequential nested region that completes, then moves to Menu |
| Load Config, Warm Up | TaskWait | Token-aware `UniTask` work that is cancelled if the chart resets or is disposed |
| Menu, Idle, Results | Simple | Event transitions (Play, Jump, Hit, Retry) |
| Gameplay | Split | Two parallel regions: the player and the waves |
| Airborne, Spawning | Wait | Callback waits completed by a coroutine; Airborne still reacts to Hit |
| More Waves? | Choice | A guarded branch with a fallback |
| Next Wave, Reset Score | Transition | Pass-through states that run an action |
| Defeated, Victory | Leave | Leave the split from either region and go to Results |

## Run it

1. Open `GameFlow.unity` and enter Play mode.
2. Open **Tools > GameLovers > Statechart Debugger** and dock it next to the Game view.
3. Use the on-screen buttons. The debugger follows the chart live:
   - **Live chart** shows nested boxes with the active states highlighted.
   - **Tree + trace** shows the state tree and the latest trace steps.

   Turn either view on or off with its toolbar toggle.

The Activity panel in the game lists the same events and state entries from `IStatechart.Traced`.
