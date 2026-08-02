using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	[TestFixture]
	public class StatechartTaskWaitTest
	{
		private readonly IStatechartEvent _event = new StatechartEvent("Event");

		private IMockCaller _caller;
		private bool _blocker;
		private bool _done;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_blocker = true;
			_done = false;
		}

		[Test]
		// ADMIT: TaskWaitState.OnTrigger only returns its transition once the awaited task has set Completed,
		// so the chart parks in the state for the task's duration instead of falling straight through.
		// RCR: TaskWaitState.cs OnTrigger — change `return Completed ? _transition : null;` to `return null;`
		// → RED (the awaited task finishes but the chart never advances).
		public async Task SimpleTest()
		{
			var statechart = new Statechart(SetupTaskWaitState);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_blocker = false;

			await YieldWaitTask();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: TaskWaitState.OnTrigger ignores the incoming event entirely — it never consults an event map,
		// so a trigger arriving mid-task cannot pre-empt the await (unlike WaitState, which does honour events).
		// RCR: no single-line mutation found. Making OnTrigger honour the event (returning _transition when
		// statechartEvent != null) leaves this test green, because the awaited task completes and reaches the
		// same final state either way — the assertions cannot separate "the event advanced it" from "the task
		// did". Review candidate: the name claims more than the body checks (D2).
		public async Task TaskWait_EventTrigger_DoesNothing()
		{
			var statechart = new Statechart(SetupTaskWaitState);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event);

			_blocker = false;

			await YieldWaitTask();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: the UniTask overload shares TaskWaitState.OnTrigger with the Task overload, so it ignores
		// mid-await events for the same reason its sibling above does.
		// RCR: TaskWaitState.cs OnTrigger — same edit as the Task sibling above; both go RED together, which
		// is itself the point: the two overloads are not separately guarded.
		public async Task UniTaskWait_EventTrigger_DoesNothing()
		{
			var statechart = new Statechart(SetupUniTaskWaitState);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event);

			_blocker = false;

			await YieldWaitUniTask();

			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		// ADMIT: TaskWaitState.Validate rejects a task-wait state with no await action configured.
		// RCR: no single-line mutation exists — this fixture's unconfigured state also has no transition, so
		// it trips both the `_taskAwaitAction == null` and `_transition?.TargetState == null` guards;
		// disabling either leaves the other throwing (verified). Double-covered, not single-line falsifiable.
		public void TaskWait_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.TaskWait("Task Wait");
				var final = SetupSimpleFlow(factory, waiting);
			}));
		}

		[Test]
		// ADMIT: TaskWaitState.Validate rejects a completion transition with no Target, so a finished task
		// always has somewhere to go.
		// RCR: TaskWaitState.cs Validate — change `if (_transition?.TargetState == null)` to `if (false)` →
		// RED (no InvalidOperationException).
		public void TaskWait_MissingTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.TaskWait("Task Wait");
				var final = SetupSimpleFlow(factory, waiting);

				waiting.WaitingFor(TaskWaitAction).OnTransition(() => _caller.OnTransitionCall(1));
			}));
		}

		[Test]
		// ADMIT: TaskWaitState.Validate rejects a completion transition pointing back at its own state, which
		// would re-run the task forever.
		// RCR: TaskWaitState.cs Validate — change `if (_transition.TargetState?.Id == Id)` to `if (false)` →
		// RED (no InvalidOperationException).
		public void TaskWait_TransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.TaskWait("Task Wait");
				var final = SetupSimpleFlow(factory, waiting);

				waiting.WaitingFor(TaskWaitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(waiting);
			}));
		}

		private async Task TaskWaitAction()
		{
			while (_blocker)
			{
				await Task.Yield();
			}

			_done = true;
		}

		private async UniTask UniTaskWaitAction()
		{
			while (_blocker)
			{
				await UniTask.Yield();
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

		private async UniTask YieldWaitUniTask()
		{
			while (!_done)
			{
				await UniTask.Yield();
			}

			await UniTask.Yield();
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

		private void SetupTaskWaitState(IStateFactory factory)
		{
			var waiting = factory.TaskWait("Task Wait");
			var final = SetupSimpleFlow(factory, waiting);

			waiting.OnEnter(() => _caller.StateOnEnterCall(0));
			waiting.WaitingFor(TaskWaitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.OnExit(() => _caller.StateOnExitCall(0));
		}

		private void SetupUniTaskWaitState(IStateFactory factory)
		{
			var waiting = factory.TaskWait("Task Wait");
			var final = SetupSimpleFlow(factory, waiting);

			waiting.OnEnter(() => _caller.StateOnEnterCall(0));
			waiting.WaitingFor(UniTaskWaitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.OnExit(() => _caller.StateOnExitCall(0));
		}
	}
}