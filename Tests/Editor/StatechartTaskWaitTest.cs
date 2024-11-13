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
		public void TaskWait_MissingConfiguration_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var waiting = factory.TaskWait("Task Wait");
				var final = SetupSimpleFlow(factory, waiting);
			}));
		}

		[Test]
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