using System;
using System.Threading.Tasks;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartWaitTest
	{
		private readonly IStatechartEvent _event = new StatechartEvent("Event");

		private IMockCaller _caller;
		private IWaitActivity activity;

		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		// ADMIT: WaitState.OnTrigger only hands back its transition once the waiting activity reports
		// IsCompleted, so the chart parks in the state until the activity is completed from outside.
		// RCR: WaitState.cs OnTrigger — change `return _waitingActivity.IsCompleted ? _transition : null;` to
		// `return null;` → RED (Complete() no longer advances; OnTransitionCall(1) never received).
		public async Task SimpleTest()
		{
			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			await Task.Yield(); // To avoid race conditions with the activity creation
			activity.Complete();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: WaitActivity.AreInnerCompleted requires EVERY split child to report IsCompleted before the
		// parent activity counts as done, so completing both children releases the wait.
		// RCR: WaitActivity.cs AreInnerCompleted — change `if (!activity.IsCompleted)` to `if (true)` (no
		// child ever counts as complete) → RED (the chart never advances even with both completed). The
		// on-hold sibling stays green: it expects no advance either way.
		public async Task SplitActivity_CompleteBoth_Success()
		{
			IWaitActivity activitySplit = null;

			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			await Task.Yield(); // To avoid race conditions with the activity creation
			activitySplit = activity.Split();
			activity.Complete();

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			activitySplit.Complete();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: WaitActivity.IsCompleted ANDs the parent's own _completed flag with AreInnerCompleted(), so
		// one finished child out of two leaves the chart waiting rather than advancing early.
		// RCR: WaitActivity.cs AreInnerCompleted — change `if (!activity.IsCompleted)` to `if (false)` so
		// every child counts as done → RED (the chart advances with one child still outstanding). Dropping
		// the inner term from IsCompleted instead does NOT redden this: the parent's own _completed is what
		// the surviving term reads, and Complete() has already set it.
		public async Task SplitActivity_CompleteOnlyOneActivity_OnHold()
		{
			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => activity = waitActivity));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			await Task.Yield(); // To avoid race conditions with the activity creation
			activity.Split();
			activity.Complete();

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: WaitState.OnTrigger lets a registered event WITH a target pre-empt the pending activity and
		// move the chart on, rather than being ignored while the state waits.
		// RCR: WaitState.cs OnTrigger — change the event-path `return transition;` to
		// `return transition.TargetState != null ? null : transition;` → RED (the targeted event no longer
		// advances). The targetless-event sibling below stays green under this edit.
		public void WaitState_EventTrigger_ForceCompleted()
		{
			var statechart = new Statechart(factory => SetupWaitState(factory, waitActivity => { }));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: a registered event WITHOUT a target still runs its OnTransition action but leaves the state
		// in place — the activity is not force-completed and no exit/enter fires.
		// RCR: WaitState.cs OnTrigger — change the event-path `return transition;` to
		// `return transition.TargetState == null ? null : transition;` → RED (OnTransitionCall(2) is never
		// evoked). The targeted-event sibling above stays green under this edit.
		public void WaitState_EventTriggerWithoutTarget_OnlyEvokesOnTransition()
		{
			var statechart = new Statechart(factory =>
			{
				var waiting = factory.Wait("Wait");
				var final = SetupSimpleFlow(factory, waiting);

				waiting.OnEnter(() => _caller.StateOnEnterCall(0));
				waiting.WaitingFor(activity => {}).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
				waiting.Event(_event).OnTransition(() => _caller.OnTransitionCall(2));
				waiting.OnExit(() => _caller.StateOnExitCall(0));
			});

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().OnTransitionCall(2);
		}

		[Test]
		// ADMIT: WaitState.Validate rejects a wait state with no WaitingFor activity, which would otherwise
		// park the chart forever with nothing able to complete it.
		// RCR: no single-line mutation exists — this fixture's unconfigured state also has no transition, so
		// it trips both the `_waitAction == null` and `_transition?.TargetState == null` guards; disabling
		// either leaves the other throwing (verified). Double-covered, not single-line falsifiable.
		public void WaitState_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.Wait("Wait");
				var final = SetupSimpleFlow(factory, waiting);
			}));
		}

		[Test]
		// ADMIT: WaitState.Validate rejects a wait state whose completion transition has no Target, so a
		// completed activity always has somewhere to go.
		// RCR: WaitState.cs Validate — change `if (_transition?.TargetState == null)` to `if (false)` → RED
		// (no InvalidOperationException).
		public void WaitState_MissingTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.Wait("Wait");
				var final = SetupSimpleFlow(factory, waiting);

				waiting.WaitingFor(waitingActivity => waitingActivity.Complete());
			}));
		}

		[Test]
		// ADMIT: WaitState.Validate rejects a completion transition pointing back at its own state, which
		// would restart the activity forever.
		// RCR: WaitState.cs Validate — change `if (_transition.TargetState?.Id == Id)` to `if (false)` → RED
		// (no InvalidOperationException).
		public void WaitState_TransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.Wait("Wait");
				var final = SetupSimpleFlow(factory, waiting);

				waiting.WaitingFor(waitingActivity => waitingActivity.Complete()).OnTransition(() => _caller.OnTransitionCall(1)).Target(waiting);
			}));
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

		private void SetupWaitState(IStateFactory factory, Action<IWaitActivity> waitAction)
		{
			var waiting = factory.Wait("Wait");
			var final = SetupSimpleFlow(factory, waiting);

			waiting.OnEnter(() => _caller.StateOnEnterCall(0));
			waiting.WaitingFor(waitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.Event(_event).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			waiting.OnExit(() => _caller.StateOnExitCall(0));
		}
	}
}