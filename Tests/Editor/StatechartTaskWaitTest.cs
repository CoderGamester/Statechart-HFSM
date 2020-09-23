using System;
using System.Threading.Tasks;
using GameLovers.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameLoversEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartTaskWaitTest
	{
		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller
		{
			void InitialOnExitCall(int id);
			void FinalOnEnterCall(int id);
			void StateOnEnterCall(int id);
			void StateOnExitCall(int id);
			void OnTransitionCall(int id);
		}
		
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");

		private IMockCaller _caller;
		private bool _blocker;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_blocker = true;
		}

		[Test]
		public void BasicSetup()
		{
			var statechart = new Statechart(factory => SetupTaskWaitState(factory, TaskWaitAction));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_blocker = false;

			Task.Run(async () =>
			{
				await Task.Delay(10);
				
				_caller.Received().OnTransitionCall(1);
				_caller.DidNotReceive().OnTransitionCall(2);
				_caller.Received().StateOnExitCall(1);
				_caller.Received().FinalOnEnterCall(0);
			});
		}

		[Test]
		public void EventTrigger()
		{
			var statechart = new Statechart(factory => SetupTaskWaitState(factory, TaskWaitAction));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event1);
			
			_blocker = false;

			Task.Run(async () =>
			{
				await Task.Delay(10);
				
				_caller.Received().OnTransitionCall(1);
				_caller.Received().OnTransitionCall(2);
				_caller.Received().StateOnExitCall(1);
				_caller.Received().FinalOnEnterCall(0);
			});
		}

		[Test]
		public void MissingTaskWaiter_ThrowsException()
		{
			Assert.Throws<MissingMethodException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var waiting = factory.TaskWait("Waiting");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				waiting.OnEnter(() => _caller.StateOnEnterCall(1));
				waiting.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		[Test]
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var waiting = factory.TaskWait("Waiting");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				waiting.OnEnter(() => _caller.StateOnEnterCall(1));
				waiting.WaitingFor(TaskWaitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(waiting);
				waiting.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		[Test]
		public void EventTrigger_WithTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var waiting = factory.TaskWait("Waiting");
				var final = factory.Final("final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				waiting.OnEnter(() => _caller.StateOnEnterCall(1));
				waiting.WaitingFor(TaskWaitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
				waiting.Event(_event1).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
				waiting.OnExit(() => _caller.StateOnExitCall(1));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		private async Task TaskWaitAction()
		{
			while (_blocker)
			{
				await Task.Delay(1);
			}
		}

		private void SetupTaskWaitState(IStateFactory factory, Func<Task> waitAction)
		{
			var initial = factory.Initial("Initial");
			var waiting = factory.TaskWait("Task Wait");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			waiting.OnEnter(() => _caller.StateOnEnterCall(1));
			waiting.WaitingFor(waitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.Event(_event1).OnTransition(() => _caller.OnTransitionCall(2));
			waiting.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}
	}
}