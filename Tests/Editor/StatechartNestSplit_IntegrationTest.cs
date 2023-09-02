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
