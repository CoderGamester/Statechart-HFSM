using System;
using System.Threading.Tasks;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartNestSplit_IntegrationTest
	{
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");

		private IMockCaller _caller;
		private IWaitActivity _activity;
		private NestedStateData _nestedStateData;
		private bool _blocker;
		private bool _done;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_nestedStateData = new NestedStateData(factory => SetupWaitState(factory, waitActivity => _activity = waitActivity));
			_blocker = true;
			_done = false;
		}

		[Test]
		// ADMIT: WaitState.ForceComplete must complete the activity WITHOUT notifying the chart, so a nest
		// force-completing an inner Wait does not re-enter Statechart.MoveNext and fire the wait's own
		// transition.
		// RCR: WaitState.cs ForceComplete — `_waitingActivity.ForceComplete();` to
		// `_waitingActivity.Complete();` → RED (the callback re-enters and OnTransitionCall(1) fires).
		// Also reddens the two Split+Wait siblings, which assert the same silence.
		public void NestedState_WaitStateInner_EventTrigger_ForceCompleteSuccess()
		{
			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.DelayForceComplete must hand the triggering event to the blocked TaskWaitState
		// so it replays when the task finishes, instead of dropping it.
		// RCR: SplitState.cs DelayForceComplete — `taskState.EnqueuEvent(statechartEvent);` to
		// `taskState.EnqueuEvent(null);` → RED (after the task completes the nest never takes its event
		// transition; OnTransitionCall(3) is never received). Also reddens the Split sibling below.
		public async Task NestedState_TaskWaitStateInner_EventTrigger_QueueEvent()
		{
			_nestedStateData.Setup = SetupTaskWaitState;

			var statechart = new Statechart(SetupNest);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_blocker = false;

			await YieldWaitTask();

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit must exclude a region parked on a LeaveState from the forced
		// FinalState.Enter(), since the leave transition already owns that region's exit.
		// RCR: SplitState.cs Exit — drop `&& !(innerState.CurrenState is LeaveState)` → RED (the leave
		// region's final hook also fires: FinalOnEnterCall(0) received 3 times, not 2). Also reddens the
		// TaskWait leave sibling below.
		public void SplitState_LeaveWaitStateInner_EventTrigger_LeaveExitSuccess()
		{
			var statechart = new Statechart(SetupLeaveSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(1).OnTransitionCall(5);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.OnTrigger must drop events arriving while the split is paused on an unfinished
		// inner task, so the pending Leave still wins once the task resolves.
		// RCR: SplitState.cs OnTrigger — `if(_isPaused && !IsAllCompleted())` to `if(false)` → RED (the
		// event transition fires instead: OnTransitionCall(3) received, (5) never). Isolated — the only
		// test whose split is already paused when the event arrives.
		public async Task SplitState_LeaveTaskWaitStateInner_EventTrigger_QueueEvent_LeaveExitSuccess()
		{
			_nestedStateData.Setup = SetupTaskWaitState;

			var statechart = new Statechart(SetupLeaveSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(5);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_blocker = false;

			await YieldWaitTask();

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(5);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.Exit must walk EVERY parallel region, so the region running alongside a
		// force-completed Wait also gets its own exit hook.
		// RCR: SplitState.cs Exit — `if (innerState.ExecuteExit)` to
		// `if (innerState.ExecuteExit && i == 0)` → RED (StateOnExitCall(0) received once, not twice).
		// Also reddens the other two-region force-complete tests; the single-region nests stay green.
		public void SplitState_WaitStateInner_EventTrigger_ForceCompleteSuccess()
		{
			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: SplitState.DelayForceComplete must pause the split while an inner TaskWaitState is still
		// running instead of completing it immediately on the event.
		// RCR: SplitState.cs DelayForceComplete — `_isPaused = true;` to `_isPaused = false;` → RED (the
		// split completes before the task finishes: OnTransitionCall(3) is received in the mid-task
		// assertion block). Also reddens the Nest and Leave task siblings.
		public async Task SplitState_TaskWaitStateInner_EventTrigger_QueueEvent_CompleteSuccess()
		{
			_nestedStateData.Setup = SetupTaskWaitState;

			var statechart = new Statechart(SetupSplit);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_blocker = false;

			await YieldWaitTask();

			_caller.Received(3).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(3).InitialOnExitCall(0);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(3).FinalOnEnterCall(0);
		}

		#region Setups

		private async Task TaskWaitAction()
		{
			while (_blocker)
			{
				await Task.Yield();
			}

			_done = true;
		}

		private async Task YieldWaitTask()
		{
			while (!_done)
			{
				await Task.Yield();
			}

			await Task.Yield();
		}

		private IFinalState SetupSimpleFlow(IStateFactory factory, IState state)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(state);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));

			return final;
		}

		private void SetupNest(IStateFactory factory)
		{
			var nest = factory.Nest("Nest");
			var final = SetupSimpleFlow(factory, nest);

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(_nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			nest.Event(_event2).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(1));
		}

		private void SetupSplit(IStateFactory factory)
		{
			var split = factory.Split("Split");
			var final = SetupSimpleFlow(factory, split);
			var nestedStateData = new NestedStateData(factory =>
			{
				var state = factory.State("State");

				SetupSimpleFlow(factory, state);

				state.OnEnter(() => _caller.StateOnEnterCall(0));
				state.OnExit(() => _caller.StateOnExitCall(0));
			});

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(_nestedStateData, nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			split.Event(_event2).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(1));
		}

		private void SetupLeaveSplit(IStateFactory factory)
		{
			var split = factory.Split("Split");
			var final = SetupSimpleFlow(factory, split);
			var nestedStateData = new NestedStateData(factory => SetupLeave(factory, final));

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(_nestedStateData, nestedStateData).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			split.Event(_event2).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(1));
		}

		private ILeaveState SetupLeave(IStateFactory factory, IState leaveTarget)
		{
			var leave = factory.Leave("Leave");
			var final = SetupSimpleFlow(factory, leave);

			leave.OnEnter(() => _caller.StateOnEnterCall(0));
			leave.Transition().OnTransition(() => _caller.OnTransitionCall(5)).Target(leaveTarget);

			return leave;
		}

		private void SetupWaitState(IStateFactory factory, Action<IWaitActivity> waitAction)
		{
			var waiting = factory.Wait("Wait");
			var final = SetupSimpleFlow(factory, waiting);

			waiting.OnEnter(() => _caller.StateOnEnterCall(0));
			waiting.WaitingFor(waitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.Event(_event1).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			waiting.OnExit(() => _caller.StateOnExitCall(0));
		}

		private void SetupTaskWaitState(IStateFactory factory)
		{
			var waiting = factory.TaskWait("Task Wait");
			var final = SetupSimpleFlow(factory, waiting);

			waiting.OnEnter(() => _caller.StateOnEnterCall(0));
			waiting.WaitingFor(TaskWaitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.OnExit(() => _caller.StateOnExitCall(0));
		}

		#endregion
	}
}
